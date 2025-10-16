namespace Business.Interfaces.Notifications
{
    /// <summary>
    /// Define un contrato para el servicio de notificaciones relacionadas 
    /// con cambios en los permisos o roles de los usuarios dentro del sistema.
    /// 
    /// Su propósito es desacoplar la lógica de negocio que gestiona los permisos 
    /// de la lógica que comunica dichos cambios a los clientes, otros servicios 
    /// o sistemas externos (por ejemplo, mediante eventos, colas o señales en tiempo real).
    /// </summary>
    public interface IPermissionsNotificationService
    {
        /// <summary>
        /// Envía una notificación cuando los permisos o roles han cambiado 
        /// para uno o varios usuarios.
        /// </summary>
        /// <param name="userIds">
        /// Colección de identificadores únicos de los usuarios cuyos permisos 
        /// fueron actualizados.
        /// </param>
        /// <remarks>
        /// Este método puede ser utilizado, por ejemplo, para:
        /// <list type="bullet">
        /// <item>Invalidar cachés de autorización en clientes activos.</item>
        /// <item>Forzar la recarga de permisos en sesiones abiertas.</item>
        /// <item>Informar a servicios dependientes o gateways de seguridad 
        /// sobre los cambios de roles.</item>
        /// </list>
        /// </remarks>
        Task NotifyPermissionsUpdated(IEnumerable<int> userIds);
    }
}
