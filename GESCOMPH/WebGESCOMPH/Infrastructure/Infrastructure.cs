using Entity.Domain.Models.Implements.SecurityAuthentication;
using Microsoft.Extensions.Options;

namespace WebGESCOMPH.Infrastructure
{
    /// <summary>
    /// Define la interfaz para la fábrica de configuración de cookies 
    /// utilizadas en el proceso de autenticación (Access, Refresh, CSRF).
    /// </summary>
    /// <remarks>
    /// Proporciona una capa de abstracción sobre las opciones de 
    /// configuración (<see cref="CookieOptions"/>) aplicadas a las cookies 
    /// emitidas por el sistema de autenticación.
    /// </remarks>
    public interface IAuthCookieFactory
    {
        /// <summary>
        /// Devuelve las opciones de cookie para el token de acceso (Access Token).
        /// </summary>
        /// <param name="expires">Fecha y hora de expiración del token.</param>
        /// <returns>Instancia configurada de <see cref="CookieOptions"/>.</returns>
        CookieOptions AccessCookieOptions(DateTimeOffset expires);

        /// <summary>
        /// Devuelve las opciones de cookie para el token de renovación (Refresh Token).
        /// </summary>
        /// <param name="expires">Fecha y hora de expiración del token.</param>
        /// <returns>Instancia configurada de <see cref="CookieOptions"/>.</returns>
        CookieOptions RefreshCookieOptions(DateTimeOffset expires);

        /// <summary>
        /// Devuelve las opciones de cookie para el token anti-CSRF (Cross-Site Request Forgery).
        /// </summary>
        /// <param name="expires">Fecha y hora de expiración del token.</param>
        /// <returns>Instancia configurada de <see cref="CookieOptions"/>.</returns>
        CookieOptions CsrfCookieOptions(DateTimeOffset expires);
    }

    /// <summary>
    /// Implementación concreta de <see cref="IAuthCookieFactory"/> 
    /// que genera cookies seguras y configuradas dinámicamente 
    /// según los parámetros definidos en <see cref="CookieSettings"/>.
    /// </summary>
    /// <remarks>
    /// Centraliza la configuración de cookies relacionadas con 
    /// autenticación JWT:
    /// <list type="bullet">
    /// <item><b>Access</b>: cookie HTTP-only con el token de acceso.</item>
    /// <item><b>Refresh</b>: cookie HTTP-only con el token de renovación.</item>
    /// <item><b>CSRF</b>: cookie legible por cliente (no HTTP-only) usada para mitigar ataques CSRF.</item>
    /// </list>
    /// 
    /// Las configuraciones se obtienen desde inyección de dependencias 
    /// usando <see cref="IOptions{TOptions}"/>.
    /// </remarks>
    public class AuthCookieFactory : IAuthCookieFactory
    {
        private readonly CookieSettings _settings;

        /// <summary>
        /// Inicializa una nueva instancia de la fábrica de cookies de autenticación.
        /// </summary>
        /// <param name="cookieOptions">Configuración inyectada desde appsettings (sección <c>CookieSettings</c>).</param>
        public AuthCookieFactory(IOptions<CookieSettings> cookieOptions)
        {
            _settings = cookieOptions.Value;
        }

        /// <inheritdoc/>
        public CookieOptions AccessCookieOptions(DateTimeOffset expires) => new()
        {
            HttpOnly = true,
            Secure = _settings.Secure,
            SameSite = SameSiteMode.None,
            Expires = expires.UtcDateTime,
            Path = _settings.Path,
            Domain = _settings.Domain
        };

        /// <inheritdoc/>
        public CookieOptions RefreshCookieOptions(DateTimeOffset expires) => new()
        {
            HttpOnly = true,
            Secure = _settings.Secure,
            SameSite = SameSiteMode.None,
            Expires = expires.UtcDateTime,
            Path = _settings.Path,
            Domain = _settings.Domain
        };

        /// <inheritdoc/>
        public CookieOptions CsrfCookieOptions(DateTimeOffset expires) => new()
        {
            HttpOnly = false, // Debe ser accesible desde JS para incluir el token CSRF en headers
            Secure = _settings.Secure,
            SameSite = SameSiteMode.None,
            Expires = expires.UtcDateTime,
            Path = _settings.Path,
            Domain = _settings.Domain
        };
    }
}
