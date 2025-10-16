namespace Entity.DTOs.Implements.SecurityAuthentication.Auth
{
    /// <summary>
    /// DTO de respuesta para la rotación de tokens (refresh).
    /// </summary>
    public class TokenRefreshResponseDto
    {
        /// <summary>
        /// Nuevo access token JWT generado.
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Nuevo refresh token (valor plano, no hash).
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Fecha y hora de expiración del nuevo access token.
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }
}
