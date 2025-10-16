using Business.Interfaces;
using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Data.Interfaz.Security;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Utilities.Exceptions;
using Utilities.Helpers.Token;

namespace Business.CustomJWT
{
    /// <summary>
    /// Implementación de <see cref="IToken"/> que gestiona la emisión, rotación y revocación de tokens JWT y Refresh Tokens.
    /// 
    /// Características:
    /// - Usa HMAC-SHA512 con pepper (JwtSettings.Key) para hashear los refresh tokens.
    /// - Incluye validación de integridad y revocación por reutilización.
    /// - Genera tokens con claims mínimos y soporte para roles.
    /// </summary>
    public class TokenBusiness : IToken
    {
        private readonly IUserRepository _userRepository;
        private readonly IRolUserRepository _rolUserRepository;
        private readonly IRefreshTokenRepository _refreshRepo;
        private readonly JwtSettings _jwtSettings;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<TokenBusiness> _logger;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="TokenBusiness"/>.
        /// </summary>
        public TokenBusiness(
            IUserRepository userRepo,
            IRolUserRepository rolRepo,
            IRefreshTokenRepository refreshRepo,
            IOptions<JwtSettings> jwtSettings,
            IPasswordHasher<User> passwordHasher,
            ILogger<TokenBusiness> logger)
        {
            _userRepository = userRepo;
            _rolUserRepository = rolRepo;
            _refreshRepo = refreshRepo;
            _jwtSettings = jwtSettings.Value;
            _passwordHasher = passwordHasher;
            _logger = logger;

            EnsureSigningKeyStrength(_jwtSettings.Key);
        }

        /// <summary>
        /// Autentica las credenciales del usuario y genera un nuevo par de tokens:
        /// Access Token (JWT), Refresh Token y CSRF Token.
        /// </summary>
        /// <param name="dto">Credenciales del usuario.</param>
        /// <returns>Tokens de acceso y renovación junto con la expiración.</returns>
        public async Task<TokenResponseDto> GenerateTokensAsync(UserAuthDto user)
        {
            var accessToken = BuildAccessToken(user);

            var refreshPlain = TokenHelpers.GenerateSecureRandomUrlToken(64);
            var refreshHash = HashRefreshToken(refreshPlain);

            await _refreshRepo.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays)
            });

            await _refreshRepo.RevokeOldTokensAsync(user.Id, _jwtSettings.MaxActiveRefreshTokens);

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshPlain,
                CsrfToken = TokenHelpers.GenerateSecureRandomUrlToken(32),
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes)
            };
        }


        /// <summary>
        /// Intercambia un Refresh Token válido por un nuevo Access Token y un nuevo Refresh Token.
        /// Detecta y revoca tokens reutilizados.
        /// </summary>
        /// <param name="dto">Datos de solicitud de refresco de token.</param>
        /// <returns>Nuevos tokens emitidos tras la rotación.</returns>
        public async Task<TokenRefreshResponseDto> RefreshAsync(TokenRefreshRequestDto dto)
        {
            try
            {
                var hash = HashRefreshToken(dto.RefreshToken);
                var record = await _refreshRepo.GetByHashAsync(hash)
                    ?? throw new SecurityTokenException("Refresh token inválido.");

                if (record.ExpiresAt <= DateTime.UtcNow)
                    throw new SecurityTokenException("Refresh token expirado.");

                if (record.IsRevoked)
                {
                    await RevokeAllActiveTokensAsync(record.UserId);
                    _logger.LogWarning("Intento de reutilización de refresh token detectado. UserId: {UserId}", record.UserId);
                    throw new SecurityTokenException("Refresh token inválido o reutilizado.");
                }

                var user = record.User
                    ?? throw new SecurityTokenException("Usuario asociado al token no encontrado.");

                var userDto = new UserAuthDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    PersonId = user.PersonId,
                    Active = user.Active,
                    IsDeleted = user.IsDeleted,
                    Roles = user.RolUsers.Select(ur => ur.Rol.Name)

                };

                var newAccessToken = BuildAccessToken(userDto);

                var now = DateTime.UtcNow;
                var newRefreshPlain = TokenHelpers.GenerateSecureRandomUrlToken(64);
                var newRefreshHash = HashRefreshToken(newRefreshPlain);

                var newRefreshEntity = new RefreshToken
                {
                    UserId = user.Id,
                    TokenHash = newRefreshHash,
                    CreatedAt = now,
                    ExpiresAt = now.AddDays(_jwtSettings.RefreshTokenExpirationDays)
                };

                await _refreshRepo.AddAsync(newRefreshEntity);
                await _refreshRepo.RevokeAsync(record, replacedByTokenHash: newRefreshHash);

                _logger.LogInformation("Token de usuario {UserId} rotado correctamente.", user.Id);

                return new TokenRefreshResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshPlain,
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


        /// <summary>
        /// Revoca todos los refresh tokens válidos de un usuario.
        /// </summary>
        private async Task RevokeAllActiveTokensAsync(int userId)
        {
            var validTokens = await _refreshRepo.GetValidTokensByUserAsync(userId);
            foreach (var t in validTokens)
                await _refreshRepo.RevokeAsync(t);
        }

        /// <summary>
        /// Revoca explícitamente un Refresh Token existente (por su valor plano).
        /// </summary>
        /// <param name="refreshToken">Valor del token a revocar.</param>
        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var hash = HashRefreshToken(refreshToken);
            var record = await _refreshRepo.GetByHashAsync(hash);

            if (record != null && !record.IsRevoked)
                await _refreshRepo.RevokeAsync(record);
        }

        /// <summary>
        /// Construye un Access Token JWT con los claims mínimos y roles asignados al usuario.
        /// </summary>
        /// <param name="user">Entidad de usuario.</param>
        /// <param name="roles">Colección de roles asociados al usuario.</param>
        /// <returns>Cadena JWT firmada.</returns>
        private string BuildAccessToken(UserAuthDto user)
        {
            var now = DateTime.UtcNow;
            var exp = now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            };

            if (user.PersonId.HasValue)
                claims.Add(new Claim("person_id", user.PersonId.Value.ToString()));

            foreach (var role in user.Roles.Distinct())
                claims.Add(new Claim(ClaimTypes.Role, role));

            var jwt = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                notBefore: now,
                expires: exp,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        /// <summary>
        /// Hashea el refresh token con HMAC-SHA512 + pepper (JwtSettings.Key).
        /// </summary>
        private string HashRefreshToken(string token)
        {
            var pepper = Encoding.UTF8.GetBytes(_jwtSettings.Key);
            using var hmac = new HMACSHA512(pepper);
            var bytes = Encoding.UTF8.GetBytes(token);
            var mac = hmac.ComputeHash(bytes);
            return Convert.ToHexString(mac).ToLowerInvariant();
        }

        /// <summary>
        /// Garantiza que la clave JWT tenga entropía suficiente (≥256 bits).
        /// </summary>
        private static void EnsureSigningKeyStrength(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
                throw new InvalidOperationException("JwtSettings.Key debe tener al menos 32 caracteres aleatorios (≥256 bits).");
        }
    }
}
