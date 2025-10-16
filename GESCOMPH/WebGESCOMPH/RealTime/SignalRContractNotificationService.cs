using Business.Interfaces.Notifications;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using WebGESCOMPH.RealTime.Contract;

namespace WebGESCOMPH.RealTime
{
    /// <summary>
    /// Implementación del servicio de notificaciones de contratos 
    /// utilizando SignalR como canal de comunicación en tiempo real.
    /// 
    /// Este servicio envía eventos a los grupos de clientes conectados 
    /// (por rol o por identificador de persona) en el <see cref="ContractsHub"/>.
    /// </summary>
    /// <remarks>
    /// Los eventos emitidos por este servicio permiten que los clientes 
    /// actualicen sus interfaces de usuario de forma inmediata al producirse 
    /// cambios en los contratos (creación, expiración, eliminación o cambio de estado).
    /// 
    /// Los grupos se asignan en <see cref="ContractsHub.OnConnectedAsync()"/> 
    /// según los roles y claims del usuario autenticado.
    /// </remarks>
    public class SignalRContractNotificationService : IContractNotificationService
    {
        private readonly IHubContext<ContractsHub> _hubContext;

        /// <summary>
        /// Inicializa una nueva instancia del servicio de notificaciones 
        /// de contratos basado en SignalR.
        /// </summary>
        /// <param name="hubContext">
        /// Contexto de SignalR (<see cref="IHubContext{THub}"/>) que permite 
        /// enviar mensajes a los clientes conectados al <see cref="ContractsHub"/>.
        /// </param>
        public SignalRContractNotificationService(IHubContext<ContractsHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// Notifica a los administradores y al arrendador correspondiente 
        /// que se ha creado un nuevo contrato.
        /// </summary>
        /// <param name="contractId">Identificador del contrato recién creado.</param>
        /// <param name="personId">Identificador de la persona asociada al contrato.</param>
        public async Task NotifyContractCreated(int contractId, int personId)
        {
            // Evento canónico usado por el frontend actual
            await _hubContext.Clients.Group($"tenant-{personId}")
                .SendAsync("contracts:mutated", new { type = "created", id = contractId, at = DateTime.UtcNow });
        }

        /// <summary>
        /// Notifica que un contrato ha expirado, informando tanto 
        /// a los administradores como al arrendador correspondiente.
        /// </summary>
        /// <param name="contractId">Identificador del contrato expirado.</param>
        /// <param name="personId">Identificador de la persona asociada al contrato.</param>
        public async Task NotifyContractExpired(int contractId, int personId)
        {
            // Mantener compatibilidad con clientes que escuchen 'contracts:expired' individuales
            await _hubContext.Clients.Group($"tenant-{personId}")
                .SendAsync("contracts:expired", new { id = contractId, at = DateTime.UtcNow });
        }

        /// <summary>
        /// Notifica a todos los clientes conectados que el estado 
        /// de un contrato ha cambiado (por ejemplo, activado o desactivado).
        /// </summary>
        /// <param name="contractId">Identificador del contrato afectado.</param>
        /// <param name="active">Estado actual del contrato (<c>true</c> si está activo).</param>
        public async Task NotifyContractStatusChanged(int contractId, bool active, int personId)
        {
            await _hubContext.Clients.Group($"tenant-{personId}")
                .SendAsync("contracts:mutated", new { type = "statusChanged", id = contractId, active, at = DateTime.UtcNow });
        }

        /// <summary>
        /// Notifica a todos los clientes conectados que un contrato ha sido eliminado.
        /// </summary>
        /// <param name="contractId">Identificador del contrato eliminado.</param>
        public async Task NotifyContractDeleted(int contractId, int personId)
        {
            await _hubContext.Clients.Group($"tenant-{personId}")
                .SendAsync("contracts:mutated", new { type = "deleted", id = contractId, at = DateTime.UtcNow });
        }
    }
}
