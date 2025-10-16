namespace Entity.DTOs.Implements.SecurityAuthentication.Auth
{
    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string CsrfToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }   // 🔹 Opcional, útil para frontend
    }

}