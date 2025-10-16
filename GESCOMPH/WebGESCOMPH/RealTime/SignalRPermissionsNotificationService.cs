using System.Linq;
using System.Threading.Tasks;
using Business.Interfaces.Notifications;
using Microsoft.AspNetCore.SignalR;
using WebGESCOMPH.RealTime.Security;

namespace WebGESCOMPH.RealTime
{
    /// <summary>
    /// Implementación del servicio de notificaciones de cambios en permisos o roles 
    /// mediante SignalR.
    /// 
    /// Este servicio permite informar en tiempo real a los clientes conectados 
    /// cuando se actualizan los permisos de uno o varios usuarios, de modo que 
    /// puedan sincronizar su estado de autenticación o sus privilegios de acceso.
    /// </summary>
    /// <remarks>
    /// La notificación se emite a todos los clientes conectados al <see cref="SecurityHub"/>,
    /// ya que los cambios en permisos suelen requerir que cada cliente verifique 
    /// si su usuario se encuentra afectado.
    /// </remarks>
    public class SignalRPermissionsNotificationService : IPermissionsNotificationService
    {
        private readonly IHubContext<SecurityHub> _hub;

        /// <summary>
        /// Inicializa una nueva instancia del servicio de notificaciones 
        /// de permisos basado en SignalR.
        /// </summary>
        /// <param name="hub">
        /// Contexto de SignalR (<see cref="IHubContext{THub}"/>) utilizado para enviar 
        /// eventos en tiempo real a los clientes conectados al <see cref="SecurityHub"/>.
        /// </param>
        public SignalRPermissionsNotificationService(IHubContext<SecurityHub> hub)
        {
            _hub = hub;
        }

        /// <summary>
        /// Envía una notificación en tiempo real indicando que los permisos 
        /// o roles han sido actualizados para ciertos usuarios.
        /// </summary>
        /// <param name="userIds">
        /// Colección de identificadores únicos de los usuarios cuyos permisos 
        /// han cambiado. Si es <c>null</c>, se envía una lista vacía.
        /// </param>
        /// <remarks>
        /// - Se eliminan duplicados mediante <see cref="Enumerable.Distinct{TSource}(IEnumerable{TSource})"/>.  
        /// - El evento emitido se llama <c>"permissions:updated"</c> y contiene un 
        /// objeto anónimo con la propiedad <c>userIds</c>.  
        /// 
        /// Este evento puede ser escuchado por los clientes para invalidar 
        /// cachés locales, refrescar tokens o actualizar vistas dependientes 
        /// de roles/privilegios.
        /// </remarks>
        public async Task NotifyPermissionsUpdated(IEnumerable<int> userIds)
        {
            var arr = (userIds ?? Enumerable.Empty<int>()).Distinct().ToArray();

            await _hub.Clients.All
                .SendAsync("permissions:updated", new { userIds = arr });
        }
    }
}
