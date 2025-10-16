using Business.CustomJWT;
using Hangfire.Dashboard;

namespace WebGESCOMPH.Security
{
    /// <summary>
    /// Implementa un filtro de autorización personalizado para el panel de control 
    /// de Hangfire.
    /// 
    /// Este filtro utiliza el servicio <see cref="ICurrentUser"/> para validar 
    /// que el usuario autenticado tenga privilegios adecuados antes de permitir 
    /// el acceso al dashboard de tareas en segundo plano.
    /// </summary>
    /// <remarks>
    /// Se restringe el acceso únicamente a usuarios con roles de 
    /// <c>Administrador</c> o <c>Arrendador</c>.  
    /// 
    /// Hangfire invoca este filtro antes de renderizar el dashboard, 
    /// por lo que las decisiones de autorización deben ser rápidas 
    /// y no bloquear el hilo de ejecución.
    /// </remarks>
    public sealed class HangfireDashboardAuth : IDashboardAuthorizationFilter
    {
        /// <summary>
        /// Determina si el usuario autenticado está autorizado para acceder 
        /// al dashboard de Hangfire.
        /// </summary>
        /// <param name="context">
        /// Contexto actual de ejecución del dashboard, que incluye 
        /// la información del <see cref="Microsoft.AspNetCore.Http.HttpContext"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> si el usuario tiene acceso autorizado; 
        /// de lo contrario, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// - Primero valida si el usuario está autenticado.  
        /// - Luego resuelve el servicio <see cref="ICurrentUser"/> desde 
        ///   el contenedor de dependencias (DI).  
        /// - Finalmente, concede acceso solo si el usuario tiene el rol 
        ///   de administrador o arrendador.
        /// </remarks>
        public bool Authorize(DashboardContext context)
        {
            var http = context.GetHttpContext();

            // Bloquear acceso si no está autenticado
            if (http.User.Identity?.IsAuthenticated != true)
                return false;

            // Resolver ICurrentUser desde el contenedor de dependencias (DI)
            var currentUser = http.RequestServices.GetService(typeof(ICurrentUser)) as ICurrentUser;
            if (currentUser is null)
                return false;

            // Solo administradores o arrendadores pueden acceder
            return currentUser.EsAdministrador || currentUser.EsArrendador;
        }
    }
}
