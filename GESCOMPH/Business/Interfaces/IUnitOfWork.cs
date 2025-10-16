using System;
using System.Threading;
using System.Threading.Tasks;

namespace Business.Interfaces
{
    /// <summary>
    /// Define un contrato para coordinar operaciones transaccionales dentro 
    /// de una unidad de trabajo (Unit of Work).
    /// 
    /// Esta interfaz abstrae la ejecución de acciones dentro de un contexto 
    /// transaccional, asegurando la atomicidad de las operaciones y permitiendo 
    /// ejecutar tareas adicionales después de un commit exitoso.
    /// 
    /// Es comúnmente usada en arquitecturas DDD o Clean Architecture para 
    /// desacoplar la capa de negocio de los detalles de infraestructura 
    /// (por ejemplo, Entity Framework, Dapper, u ORMs personalizados).
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Ejecuta una acción asincrónica dentro de una transacción.
        /// </summary>
        /// <param name="action">
        /// Función asincrónica que contiene las operaciones a ejecutar 
        /// dentro de la transacción.
        /// </param>
        /// <param name="ct">Token de cancelación opcional.</param>
        /// <returns>
        /// Una tarea que representa la ejecución completa de la acción 
        /// transaccional.
        /// </returns>
        /// <remarks>
        /// Si ocurre una excepción durante la ejecución, la transacción 
        /// debe revertirse (rollback).  
        /// 
        /// Este método se utiliza cuando la operación no produce un valor 
        /// de retorno.
        /// </remarks>
        Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);

        /// <summary>
        /// Ejecuta una acción asincrónica dentro de una transacción y devuelve 
        /// un resultado de tipo <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Tipo del resultado devuelto por la acción.</typeparam>
        /// <param name="action">
        /// Función asincrónica que contiene las operaciones a ejecutar 
        /// dentro de la transacción.
        /// </param>
        /// <param name="ct">Token de cancelación opcional.</param>
        /// <returns>
        /// Una tarea que devuelve un resultado de tipo <typeparamref name="T"/> 
        /// si la transacción se completa exitosamente.
        /// </returns>
        /// <remarks>
        /// Si ocurre una excepción o la operación es cancelada, la transacción 
        /// debe revertirse.  
        /// 
        /// Este método es ideal cuando se requiere devolver datos generados o 
        /// calculados dentro del mismo contexto transaccional.
        /// </remarks>
        Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default);

        /// <summary>
        /// Registra una acción que se ejecutará automáticamente después de que 
        /// la transacción se confirme exitosamente (post-commit).
        /// </summary>
        /// <param name="action">
        /// Función asincrónica que define la lógica a ejecutar tras un commit 
        /// exitoso, como envío de notificaciones, eventos de dominio, logs, etc.
        /// </param>
        /// <remarks>
        /// Este mecanismo permite aplicar el patrón *Outbox* o *Post-Commit Hook*, 
        /// ejecutando tareas que deben realizarse solo si la transacción fue 
        /// confirmada correctamente.
        /// 
        /// Las acciones post-commit no deben modificar el estado transaccional, 
        /// ya que se ejecutan fuera del contexto de base de datos.
        /// </remarks>
        void RegisterPostCommit(Func<CancellationToken, Task> action);
    }
}
