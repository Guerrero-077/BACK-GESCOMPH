using Data.Interfaz.Security;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.SecurityAuthentication
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _ctx;
        private readonly DbSet<RefreshToken> _dbSet;

        public RefreshTokenRepository(ApplicationDbContext ctx)
        {
            _ctx = ctx;
            _dbSet = _ctx.Set<RefreshToken>();
        }

        /// <summary>
        /// Agrega un nuevo refresh token a la base de datos.
        /// </summary>
        public async Task AddAsync(RefreshToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            // Aseguramos inicialización mínima
            token.CreatedAt = DateTime.UtcNow;
            token.IsRevoked = false;

            await _dbSet.AddAsync(token);
            await _ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Obtiene un token por su hash (solo lectura).
        /// </summary>
        public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
                return null;

            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        }

        /// <summary>
        /// Obtiene todos los tokens válidos de un usuario (no revocados ni expirados),
        /// ordenados por fecha de creación descendente.
        /// </summary>
        public async Task<IEnumerable<RefreshToken>> GetValidTokensByUserAsync(int userId)
        {
            var now = DateTime.UtcNow;

            var tokens = await _dbSet
                .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > now)
                .OrderByDescending(t => t.CreatedAt)
                .AsNoTracking()
                .ToListAsync();

            return tokens;
        }

        /// <summary>
        /// Revoca un token, opcionalmente marcando cuál lo reemplazó.
        /// </summary>
        public async Task RevokeAsync(RefreshToken token, string? replacedByTokenHash = null)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            token.IsRevoked = true;
            token.ReplacedByTokenHash = replacedByTokenHash;

            _dbSet.Update(token);
            await _ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Revoca los tokens más antiguos, manteniendo un máximo permitido por usuario.
        /// (Método adicional interno, pero puede llamarse desde el Business.)
        /// </summary>
        public async Task RevokeOldTokensAsync(int userId, int maxTokens)
        {
            var now = DateTime.UtcNow;

            var oldTokens = await _dbSet
                .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > now)
                .OrderByDescending(t => t.CreatedAt)
                .Skip(maxTokens)
                .ToListAsync();

            if (oldTokens.Count == 0)
                return;

            foreach (var token in oldTokens)
                token.IsRevoked = true;

            _dbSet.UpdateRange(oldTokens);
            await _ctx.SaveChangesAsync();
        }
    }
}
