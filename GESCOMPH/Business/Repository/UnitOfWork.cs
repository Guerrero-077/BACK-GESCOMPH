using Business.Interfaces;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Business.Repository
{
    /// <summary>
    /// Implementaci�n del patr�n <b>Unit of Work</b> (UoW) para coordinar operaciones transaccionales
    /// sobre el contexto de base de datos <see cref="ApplicationDbContext"/>.
    /// 
    /// Esta clase:
    /// - Encapsula la gesti�n de transacciones.
    /// - Aplica estrategias de reintento configuradas en EF Core.
    /// - Permite registrar acciones post-commit (<see cref="RegisterPostCommit"/>) que se ejecutan
    ///   s�lo si la transacci�n se completa exitosamente.
    /// 
    /// Es <b>sealed</b> para evitar herencia y garantizar la consistencia del ciclo de vida transaccional.
    /// </summary>
    public sealed class UnitOfWork : IUnitOfWork
    {
        /// <summary>
        /// Contexto de base de datos de EF Core.
        /// </summary>
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Logger opcional para registrar errores o eventos del flujo transaccional.
        /// </summary>
        private readonly ILogger<UnitOfWork>? _logger;

        /// <summary>
        /// Lista de acciones que deben ejecutarse despu�s de un commit exitoso.
        /// </summary>
        private readonly List<Func<CancellationToken, Task>> _postCommitActions = new();

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="UnitOfWork"/>.
        /// </summary>
        /// <param name="context">Instancia del contexto de datos (<see cref="ApplicationDbContext"/>).</param>
        /// <param name="logger">Instancia opcional de logger para registrar errores y diagn�sticos.</param>
        public UnitOfWork(ApplicationDbContext context, ILogger<UnitOfWork>? logger = null)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Ejecuta una acci�n dentro de una transacci�n con estrategia de reintento (retry) de EF Core.
        /// 
        /// Si la acci�n se ejecuta exitosamente:
        /// - La transacci�n se confirma (Commit).
        /// - Se ejecutan las acciones registradas con <see cref="RegisterPostCommit"/>.
        /// 
        /// En caso de excepci�n:
        /// - Se revierte la transacci�n (Rollback).
        /// - Se limpia la cola de acciones post-commit.
        /// </summary>
        /// <param name="action">Funci�n asincr�nica que representa la operaci�n a ejecutar dentro de la transacci�n.</param>
        /// <param name="ct">Token de cancelaci�n opcional.</param>
        /// <exception cref="Exception">Propaga cualquier error ocurrido dentro de la acci�n ejecutada.</exception>
        public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync(ct);
                try
                {
                    await action(ct);
                    await tx.CommitAsync(ct);
                    await RunPostCommitAsync(ct);
                }
                catch
                {
                    _postCommitActions.Clear();
                    await tx.RollbackAsync(ct);
                    throw;
                }
            });
        }

        /// <summary>
        /// Ejecuta una acci�n que retorna un valor dentro de una transacci�n con soporte de reintento.
        /// 
        /// Su comportamiento es id�ntico a <see cref="ExecuteAsync(Func{CancellationToken, Task})"/>,
        /// pero devuelve un resultado de tipo gen�rico.
        /// </summary>
        /// <typeparam name="T">Tipo del valor de retorno.</typeparam>
        /// <param name="action">Funci�n asincr�nica que representa la operaci�n transaccional a ejecutar.</param>
        /// <param name="ct">Token de cancelaci�n opcional.</param>
        /// <returns>Resultado de la operaci�n ejecutada dentro de la transacci�n.</returns>
        /// <exception cref="Exception">Propaga cualquier error ocurrido durante la ejecuci�n.</exception>
        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync(ct);
                try
                {
                    var result = await action(ct);
                    await tx.CommitAsync(ct);
                    await RunPostCommitAsync(ct);
                    return result;
                }
                catch
                {
                    _postCommitActions.Clear();
                    await tx.RollbackAsync(ct);
                    throw;
                }
            });
        }

        /// <summary>
        /// Registra una acci�n que se ejecutar� �nicamente despu�s de un commit exitoso.
        /// 
        /// Este mecanismo es �til para:
        /// - Env�o de notificaciones o eventos de dominio.
        /// - Sincronizaciones externas (por ejemplo, colas o servicios externos).
        /// 
        /// Si la transacci�n se revierte, las acciones registradas se descartan autom�ticamente.
        /// </summary>
        /// <param name="action">Funci�n asincr�nica a ejecutar despu�s del commit.</param>
        public void RegisterPostCommit(Func<CancellationToken, Task> action)
        {
            if (action is null) return;
            _postCommitActions.Add(action);
        }

        /// <summary>
        /// Ejecuta todas las acciones registradas post-commit de forma secuencial.
        /// 
        /// Si alguna acci�n falla, se captura la excepci�n, se registra con el <see cref="_logger"/> (si est� disponible),
        /// y se contin�a con las restantes (sin interrumpir el flujo).
        /// </summary>
        /// <param name="ct">Token de cancelaci�n opcional.</param>
        private async Task RunPostCommitAsync(CancellationToken ct)
        {
            if (_postCommitActions.Count == 0) return;

            var actions = _postCommitActions.ToArray();
            _postCommitActions.Clear();

            foreach (var act in actions)
            {
                try
                {
                    await act(ct);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Post-commit action failed");
                }
            }
        }
    }
}
