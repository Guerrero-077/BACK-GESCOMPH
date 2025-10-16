using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using System.Threading.Tasks;

namespace Business.Interfaces
{
    /// <summary>
    /// Establece un contrato para la gestión de tokens de autenticación y autorización,
    /// incluyendo su generación, renovación y revocación.
    ///
    /// Este servicio abstrae la lógica relacionada con el ciclo de vida de los tokens JWT,
    /// sus equivalentes de actualización (refresh tokens) y la emisión de tokens antifalsificación (CSRF)
    /// para entornos web seguros.
    /// </summary>
    public interface IToken
    {
        /// <summary>
        /// Valida las credenciales del usuario y genera el conjunto de tokens necesarios
        /// para la autenticación y autorización.
        /// </summary>
        /// <param name="user">
        /// Objeto <see cref="UserAuthDto"/> que contiene las credenciales de acceso
        /// (usuario, contraseña u otros datos requeridos para la autenticación).
        /// </param>
        /// <returns>
        /// Un objeto <see cref="TokenResponseDto"/> que encapsula los tokens generados:
        /// <list type="bullet">
        /// <item><term>AccessToken</term>: Token JWT principal utilizado para autenticación.</item>
        /// <item><term>RefreshToken</term>: Token seguro utilizado para renovar el acceso sin reingresar credenciales.</item>
        /// <item><term>CsrfToken</term>: Token antifalsificación (CSRF) para peticiones web seguras.</item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// Este método debe:
        /// <list type="number">
        /// <item>Validar la identidad del usuario mediante las credenciales provistas.</item>
        /// <item>Emitir tokens firmados con las claves y configuraciones establecidas.</item>
        /// <item>Asignar tiempos de expiración apropiados (corto para el Access Token y extendido para el Refresh Token).</item>
        /// </list>
        /// </remarks>
        Task<TokenResponseDto> GenerateTokensAsync(UserAuthDto user);

        /// <summary>
        /// Renueva los tokens de autenticación (Access y Refresh) utilizando un refresh token válido.
        /// </summary>
        /// <param name="dto">
        /// Objeto <see cref="TokenRefreshRequestDto"/> que contiene la información necesaria
        /// para solicitar la renovación del token, incluyendo el refresh token actual y,
        /// opcionalmente, la dirección IP o metadatos del cliente.
        /// </param>
        /// <returns>
        /// Un objeto <see cref="TokenRefreshResponseDto"/> que encapsula los nuevos tokens emitidos:
        /// <list type="bullet">
        /// <item><term>AccessToken</term>: Nuevo token JWT válido para autenticación.</item>
        /// <item><term>RefreshToken</term>: Nuevo token de actualización que reemplaza al anterior.</item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// Este método implementa el flujo de rotación segura de refresh tokens, garantizando que:
        /// <list type="bullet">
        /// <item>Cada refresh token sea de un solo uso (<i>one-time use</i>).</item>
        /// <item>Los tokens antiguos sean invalidados al emitir nuevos.</item>
        /// <item>Se rechacen tokens expirados, inválidos o previamente utilizados.</item>
        /// </list>
        /// Además, puede registrar eventos de auditoría de seguridad utilizando los metadatos del cliente
        /// (por ejemplo, dirección IP o dispositivo).
        /// </remarks>
        Task<TokenRefreshResponseDto> RefreshAsync(TokenRefreshRequestDto dto);

        /// <summary>
        /// Revoca explícitamente un refresh token, invalidándolo para futuros usos.
        /// </summary>
        /// <param name="refreshToken">Refresh token que se desea revocar.</param>
        /// <returns>Una tarea que representa la operación asíncrona de revocación.</returns>
        /// <remarks>
        /// Este método se utiliza para cerrar sesiones activas, invalidar tokens comprometidos
        /// o finalizar manualmente un flujo de autenticación.
        ///
        /// En implementaciones seguras, la revocación implica marcar el token como inválido
        /// en el almacenamiento persistente o eliminarlo de la lista blanca de tokens válidos.
        /// </remarks>
        Task RevokeRefreshTokenAsync(string refreshToken);
    }
}
