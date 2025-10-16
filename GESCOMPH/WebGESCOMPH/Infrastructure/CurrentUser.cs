using Business.CustomJWT;
using Microsoft.AspNetCore.Http;

namespace WebGESCOMPH.Infrastructure
{
    /// <summary>
    /// Implementación concreta de <see cref="ICurrentUser"/> que obtiene 
    /// la información del usuario autenticado a partir del contexto HTTP actual.
    /// 
    /// Esta clase se integra con el middleware de autenticación de ASP.NET Core 
    /// (por ejemplo, JWT Bearer) y extrae los datos de los claims contenidos 
    /// en el token del usuario.
    /// 
    /// Se declara como <c>sealed</c> para evitar herencia y mantener la 
    /// inmutabilidad del comportamiento.
    /// </summary>
    public sealed class CurrentUser : ICurrentUser
    {
        private readonly IHttpContextAccessor _ctx;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="CurrentUser"/>.
        /// </summary>
        /// <param name="ctx">
        /// Accesor al contexto HTTP (<see cref="IHttpContextAccessor"/>), 
        /// utilizado para acceder al usuario autenticado y sus claims.
        /// </param>
        public CurrentUser(IHttpContextAccessor ctx) => _ctx = ctx;

        /// <summary>
        /// Obtiene el identificador de persona (<c>person_id</c>) del usuario actual, 
        /// si existe en los claims del token JWT.
        /// </summary>
        /// <remarks>
        /// Si el claim <c>person_id</c> no está presente o no puede convertirse 
        /// a un número entero, devuelve <c>null</c>.
        /// </remarks>
        public int? PersonId =>
            int.TryParse(_ctx.HttpContext?.User?.FindFirst("person_id")?.Value, out var id) ? id : null;

        /// <summary>
        /// Verifica si el usuario autenticado pertenece al rol especificado.
        /// </summary>
        /// <param name="role">Nombre del rol a verificar.</param>
        /// <returns>
        /// <c>true</c> si el usuario pertenece al rol indicado; 
        /// de lo contrario, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Este método utiliza la funcionalidad estándar de <see cref="ClaimsPrincipal.IsInRole(string)"/> 
        /// para determinar la pertenencia a roles definidos en el token JWT.
        /// </remarks>
        public bool IsInRole(string role) =>
            _ctx.HttpContext?.User?.IsInRole(role) == true;

        /// <summary>
        /// Indica si el usuario autenticado tiene el rol de administrador.
        /// </summary>
        /// <remarks>
        /// Esta propiedad es un atajo semántico que utiliza 
        /// <see cref="IsInRole(string)"/> con el rol definido en 
        /// <see cref="AppRoles.Administrador"/>.
        /// </remarks>
        public bool EsAdministrador => IsInRole(AppRoles.Administrador);

        /// <summary>
        /// Indica si el usuario autenticado tiene el rol de arrendador.
        /// </summary>
        /// <remarks>
        /// Similar a <see cref="EsAdministrador"/>, esta propiedad simplifica 
        /// la verificación de privilegios para usuarios con el rol de 
        /// <see cref="AppRoles.Arrendador"/>.
        /// </remarks>
        public bool EsArrendador => IsInRole(AppRoles.Arrendador);
    }
}
