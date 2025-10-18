using Business.CustomJWT;
using Business.Interfaces.Implements.SecurityAuthentication.Tokens;
using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Data.Interfaz.Security;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Business.Services.SecurityAuthentication.Tokens
{
    /// <summary>
    /// Gestión de Refresh Tokens: emisión, rotación segura y revocación.
    /// </summary>
    public sealed class RefreshTokenManager : IRefreshTokenManager
    {
        private readonly IRefreshTokenRepository _refreshRepo;
        private readonly IUserRepository _userRepo;
        private readonly ITokenHasher _hasher;
        private readonly IRandomTokenGenerator _rng;
        private readonly JwtSettings _settings;
        private readonly IClock _clock;

        public RefreshTokenManager(
            IRefreshTokenRepository refreshRepo,
            IUserRepository userRepo,
            ITokenHasher hasher,
            IRandomTokenGenerator rng,
            IOptions<JwtSettings> settings,
            IClock clock)
        {
            _refreshRepo = refreshRepo;
            _userRepo = userRepo;
            _hasher = hasher;
            _rng = rng;
            _settings = settings.Value;
            _clock = clock;
        }

        public async Task<RefreshIssueResult> IssueAsync(int userId)
        {
            var now = _clock.UtcNow;
            var plain = _rng.Generate(64);
            var hash = _hasher.Hash(plain);

            var entity = new RefreshToken
            {
                UserId = userId,
                TokenHash = hash,
                CreatedAt = now,
                ExpiresAt = now.AddDays(_settings.RefreshTokenExpirationDays)
            };

            await _refreshRepo.AddAsync(entity);
            await _refreshRepo.RevokeOldTokensAsync(userId, _settings.MaxActiveRefreshTokens);

            return new RefreshIssueResult(plain, hash, entity.ExpiresAt);
        }

        public async Task<RefreshRotateResult> RotateAsync(string currentPlain)
        {
            var now = _clock.UtcNow;
            var currentHash = _hasher.Hash(currentPlain);
            var record = await _refreshRepo.GetByHashAsync(currentHash)
                         ?? throw new SecurityTokenException("Refresh token inválido.");

            if (record.ExpiresAt <= now)
                throw new SecurityTokenException("Refresh token expirado.");

            if (record.IsRevoked)
            {
                await RevokeAllAsync(record.UserId);
                throw new SecurityTokenException("Refresh token inválido o reutilizado.");
            }

            var user = await _userRepo.GetByIdWithDetailsAsync(record.UserId)
                       ?? throw new SecurityTokenException("Usuario asociado al token no encontrado.");

            var newPlain = _rng.Generate(64);
            var newHash = _hasher.Hash(newPlain);

            var newEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newHash,
                CreatedAt = now,
                ExpiresAt = now.AddDays(_settings.RefreshTokenExpirationDays)
            };

            await _refreshRepo.AddAsync(newEntity);
            await _refreshRepo.RevokeAsync(record, replacedByTokenHash: newHash);

            var userDto = new UserAuthDto
            {
                Id = user.Id,
                Email = user.Email,
                PersonId = user.PersonId,
                Active = user.Active,
                IsDeleted = user.IsDeleted,
                Roles = user.RolUsers.Select(ru => ru.Rol.Name)
            };

            return new RefreshRotateResult(userDto, newPlain, newHash, newEntity.ExpiresAt);
        }

        public async Task RevokeAsync(string plainToken)
        {
            var hash = _hasher.Hash(plainToken);
            var record = await _refreshRepo.GetByHashAsync(hash);
            if (record != null && !record.IsRevoked)
                await _refreshRepo.RevokeAsync(record);
        }

        public async Task RevokeAllAsync(int userId)
        {
            var validTokens = await _refreshRepo.GetValidTokensByUserAsync(userId);
            foreach (var t in validTokens)
                await _refreshRepo.RevokeAsync(t);
        }
    }
}