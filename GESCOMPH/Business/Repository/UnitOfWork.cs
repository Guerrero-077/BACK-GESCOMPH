using Business.Interfaces;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Business.Repository
{
    /// <summary>
    /// Implementación del patrón <b>Unit of Work</b> (UoW) para coordinar operaciones transaccionales
    /// sobre el contexto de base de datos <see cref="ApplicationDbContext"/>.
    /// 
    /// Esta clase:
    /// - Encapsula la gestión de transacciones.
    /// - Aplica estrategias de reintento configuradas en EF Core.
    /// - Permite registrar acciones post-commit (<see cref="RegisterPostCommit"/>) que se ejecutan
    ///   sólo si la transacción se completa exitosamente.
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
        /// Lista de acciones que deben ejecutarse después de un commit exitoso.
        /// </summary>
        private readonly List<Func<CancellationToken, Task>> _postCommitActions = new();

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="UnitOfWork"/>.
        /// </summary>
        /// <param name="context">Instancia del contexto de datos (<see cref="ApplicationDbContext"/>).</param>
        /// <param name="logger">Instancia opcional de logger para registrar errores y diagnósticos.</param>
        public UnitOfWork(ApplicationDbContext context, ILogger<UnitOfWork>? logger = null)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Ejecuta una acción dentro de una transacción con estrategia de reintento (retry) de EF Core.
        /// 
        /// Si la acción se ejecuta exitosamente:
        /// - La transacción se confirma (Commit).
        /// - Se ejecutan las acciones registradas con <see cref="RegisterPostCommit"/>.
        /// 
        /// En caso de excepción:
        /// - Se revierte la transacción (Rollback).
        /// - Se limpia la cola de acciones post-commit.
        /// </summary>
        /// <param name="action">Función asincrónica que representa la operación a ejecutar dentro de la transacción.</param>
        /// <param name="ct">Token de cancelación opcional.</param>
        /// <exception cref="Exception">Propaga cualquier error ocurrido dentro de la acción ejecutada.</exception>
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
        /// Ejecuta una acción que retorna un valor dentro de una transacción con soporte de reintento.
        /// 
        /// Su comportamiento es idéntico a <see cref="ExecuteAsync(Func{CancellationToken, Task})"/>,
        /// pero devuelve un resultado de tipo genérico.
        /// </summary>
        /// <typeparam name="T">Tipo del valor de retorno.</typeparam>
        /// <param name="action">Función asincrónica que representa la operación transaccional a ejecutar.</param>
        /// <param name="ct">Token de cancelación opcional.</param>
        /// <returns>Resultado de la operación ejecutada dentro de la transacción.</returns>
        /// <exception cref="Exception">Propaga cualquier error ocurrido durante la ejecución.</exception>
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
        /// Registra una acción que se ejecutará únicamente después de un commit exitoso.
        /// 
        /// Este mecanismo es útil para:
        /// - Envío de notificaciones o eventos de dominio.
        /// - Sincronizaciones externas (por ejemplo, colas o servicios externos).
        /// 
        /// Si la transacción se revierte, las acciones registradas se descartan automáticamente.
        /// </summary>
        /// <param name="action">Función asincrónica a ejecutar después del commit.</param>
        public void RegisterPostCommit(Func<CancellationToken, Task> action)
        {
            if (action is null) return;
            _postCommitActions.Add(action);
        }

        /// <summary>
        /// Ejecuta todas las acciones registradas post-commit de forma secuencial.
        /// 
        /// Si alguna acción falla, se captura la excepción, se registra con el <see cref="_logger"/> (si está disponible),
        /// y se continúa con las restantes (sin interrumpir el flujo).
        /// </summary>
        /// <param name="ct">Token de cancelación opcional.</param>
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
