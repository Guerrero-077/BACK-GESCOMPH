using Business.Interfaces.Notifications;
using WebGESCOMPH.RealTime;

namespace WebGESCOMPH.Extensions.Modules.Notifications
{
    /// <summary>
    /// Registro DI de servicios de notificaciones (SignalR adapters).
    /// </summary>
    /// <remarks>
    /// Conecta interfaces de notificación de dominio con implementaciones en tiempo real.
    /// </remarks>
    public static class NotificationsModuleExtensions
    {
        public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
        {
            services.AddScoped<IContractNotificationService, SignalRContractNotificationService>();
            services.AddScoped<IPermissionsNotificationService, SignalRPermissionsNotificationService>();
            return services;
        }
    }
}
