using WebGESCOMPH.Extensions.Modules.Administration;
using WebGESCOMPH.Extensions.Modules.Business;
using WebGESCOMPH.Extensions.Modules.Core;
using WebGESCOMPH.Extensions.Modules.Exceptions;
using WebGESCOMPH.Extensions.Modules.Location;
using WebGESCOMPH.Extensions.Modules.Notifications;
using WebGESCOMPH.Extensions.Modules.Persons;
using WebGESCOMPH.Extensions.Modules.Security;
using WebGESCOMPH.Extensions.Modules.Utilities;

namespace WebGESCOMPH.Extensions.Composition
{
    /// <summary>
    /// Ensambla los módulos de la aplicación (dominio) en el contenedor DI.
    /// </summary>
    /// <remarks>
    /// Qué hace: agrega Core + módulos por feature (Seguridad, Business, Location, AdminSystem,
    /// Utilities, Persons) y el módulo de excepciones y notificaciones.
    /// 
    /// Por qué: separar el registro de servicios del dominio de la infraestructura y presentación.
    /// 
    /// Para qué: punto único para activar/desactivar módulos y mantener Program.cs minimalista.
    /// </remarks>
    public static class ApplicationService
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Núcleo cross-cutting
            services.AddCoreModule();

            // Registro modular por features
            services
                .AddSecurityAuthenticationModule()
                .AddBusinessModule()
                .AddLocationModule()
                .AddAdministrationSystemModule()
                .AddUtilitiesModule()
                .AddPersonsModule()
                .AddExceptionHandlersModule();

            // Notificaciones (SignalR en Web)
            services.AddNotificationsModule();

            return services;
        }
    }
}

