using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Business.CustomJWT;

namespace WebGESCOMPH.RealTime.Contract
{
    /// <summary>
    /// Hub de SignalR encargado de gestionar las conexiones en tiempo real 
    /// relacionadas con los contratos.
    /// 
    /// Este hub permite asociar cada conexión a uno o varios grupos lógicos 
    /// (por ejemplo, administradores o arrendadores) basándose en los claims 
    /// del usuario autenticado.
    /// 
    /// La agrupación facilita el envío de notificaciones dirigidas únicamente 
    /// a los usuarios relevantes (por rol o por identificador de persona).
    /// </summary>
    public class ContractsHub : Hub
    {
        private readonly ICurrentUser _currentUser;

        public ContractsHub(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }
        /// <summary>
        /// Se ejecuta cuando un cliente establece una conexión con el hub.
        /// 
        /// Si el usuario está autenticado, se agrega su conexión a grupos 
        /// específicos según su rol o identificador de persona (tenant).
        /// </summary>
        /// <returns>Una tarea asincrónica que representa la finalización del proceso de conexión.</returns>
        /// <remarks>
        /// - Los administradores se agregan al grupo <c>"Admin"</c>.  
        /// - Los usuarios con un claim <c>person_id</c> válido se agregan a un 
        ///   grupo con formato <c>"tenant-{personId}"</c>.  
        /// 
        /// Esta lógica permite emitir mensajes personalizados a usuarios o 
        /// conjuntos específicos sin necesidad de mantener listas manuales de conexiones.
        /// </remarks>
        public override async Task OnConnectedAsync()
        {
            var user = Context.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                // Agrupa administradores autenticados en el grupo "Admin"
                if (_currentUser.EsAdministrador)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, "Admin");
                }

                // Agrupa al usuario por su identificador de persona (tenant)
                var pid = _currentUser.PersonId;
                if (pid.HasValue && pid.Value > 0)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant-{pid.Value}");
                }
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Se ejecuta cuando un cliente se desconecta del hub.
        /// </summary>
        /// <param name="exception">
        /// Excepción opcional que causó la desconexión, si la hubo.
        /// </param>
        /// <returns>Una tarea asincrónica que representa la finalización del proceso de desconexión.</returns>
        /// <remarks>
        /// No es necesario eliminar manualmente la conexión de los grupos, 
        /// ya que SignalR limpia las referencias automáticamente al desconectarse 
        /// el cliente.
        /// </remarks>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
