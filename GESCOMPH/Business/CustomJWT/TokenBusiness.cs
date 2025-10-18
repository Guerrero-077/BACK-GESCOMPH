using Business.Interfaces;
using Business.Interfaces.Implements.SecurityAuthentication.Tokens;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Utilities.Exceptions;

namespace Business.CustomJWT
{
    /// <summary>
    /// Servicio de tokens que orquesta la emisión de access/refresh tokens y su rotación segura.
    /// </summary>
    public class TokenBusiness : IToken
    {
        private readonly IAccessTokenFactory _accessFactory;
        private readonly IRefreshTokenManager _refreshManager;
        private readonly IRandomTokenGenerator _rng;
        private readonly IClock _clock;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<TokenBusiness> _logger;

        public TokenBusiness(
            IAccessTokenFactory accessFactory,
            IRefreshTokenManager refreshManager,
            IRandomTokenGenerator rng,
            IOptions<JwtSettings> jwtSettings,
            IClock clock,
            ILogger<TokenBusiness> logger)
        {
            _accessFactory = accessFactory;
            _refreshManager = refreshManager;
            _rng = rng;
            _jwtSettings = jwtSettings.Value;
            _clock = clock;
            _logger = logger;

            EnsureSigningKeyStrength(_jwtSettings.Key);
        }

        public async Task<TokenResponseDto> GenerateTokensAsync(UserAuthDto user)
        {
            var now = _clock.UtcNow;
            var accessToken = _accessFactory.Create(user);
            var refresh = await _refreshManager.IssueAsync(user.Id);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refresh.Plain,
                CsrfToken = _rng.Generate(32),
                ExpiresAt = now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            };
        }

        public async Task<TokenRefreshResponseDto> RefreshAsync(TokenRefreshRequestDto dto)
        {
            try
            {
                var now = _clock.UtcNow;
                var rotated = await _refreshManager.RotateAsync(dto.RefreshToken);
                var newAccessToken = _accessFactory.Create(rotated.User);

                _logger.LogInformation("Token de usuario {UserId} rotado correctamente.", rotated.User.Id);

                return new TokenRefreshResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = rotated.NewPlain,
                    ExpiresAt = now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
                };
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Error de seguridad al refrescar token: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al refrescar token.");
                throw new BusinessException("Ocurrió un error interno durante la renovación del token.", ex);
            }
        }

        public Task RevokeRefreshTokenAsync(string refreshToken)
            => _refreshManager.RevokeAsync(refreshToken);

        private static void EnsureSigningKeyStrength(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
                throw new InvalidOperationException("JwtSettings.Key debe tener al menos 32 caracteres aleatorios (=256 bits).");
        }
    }
}

