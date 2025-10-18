using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Data.Repository;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.SecurityAuthentication
{
    public class UserRepository : DataGeneric<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }

        public override IQueryable<User> GetAllQueryable()
        {
            return _dbSet
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .ThenByDescending(u => u.Id)
                .Include(u => u.Person)
                    .ThenInclude(p => p.City)
                .Include(u => u.RolUsers)
                    .ThenInclude(ru => ru.Rol);
        }

        public override async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .OrderByDescending(u => u.CreatedAt)
                .ThenByDescending(u => u.Id)
                .Include(u => u.Person)
                    .ThenInclude(p => p.City)
                .Include(u => u.RolUsers)
                    .ThenInclude(ru => ru.Rol)
                .ToListAsync();
        }

        public override async Task<User?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        public async Task<User?> GetByIdForUpdateAsync(int id)
        {
            return await _dbSet
                .Include(u => u.Person)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        public async Task<User?> GetByIdWithDetailsAsync(int id)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(u => u.Person)
                    .ThenInclude(p => p.City)
                .Include(u => u.RolUsers)
                    .ThenInclude(ru => ru.Rol)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();
            return await _dbSet.AnyAsync(u => !u.IsDeleted && u.Email.ToLower() == normalizedEmail);
        }

        public async Task<bool> ExistsByEmailExcludingIdAsync(int id, string email)
        {
            var normalizedEmail = email.Trim().ToLower();
            return await _dbSet.AnyAsync(u => !u.IsDeleted && u.Id != id && u.Email.ToLower() == normalizedEmail);
        }

        public async Task<int?> GetIdByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();

            return await _dbSet
                .Where(u => !u.IsDeleted && u.Email.ToLower() == normalizedEmail)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();

            return await _dbSet
                .Include(u => u.Person)
                .Include(u => u.RolUsers)
                    .ThenInclude(ru => ru.Rol)
                .FirstOrDefaultAsync(u => !u.IsDeleted && u.Email.ToLower() == normalizedEmail);
        }

        public async Task<User?> GetByEmailProjectionAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();

            return await _dbSet
                .AsNoTracking()
                .Where(u => !u.IsDeleted && u.Email.ToLower() == normalizedEmail)
                .Select(u => new User
                {
                    Id = u.Id,
                    Email = u.Email,
                    Password = u.Password,
                    PersonId = u.PersonId,
                    Active = u.Active,
                    IsDeleted = u.IsDeleted
                })
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetAuthUserByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLower();

            return await _dbSet
                .AsNoTracking()
                .Where(u => !u.IsDeleted && u.Email.ToLower() == normalizedEmail)
                .Select(u => new User
                {
                    Id = u.Id,
                    Email = u.Email,
                    Password = u.Password,
                    PersonId = u.PersonId
                })
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetByPersonIdAsync(int personId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(u => u.Person)
                .Include(u => u.RolUsers)
                    .ThenInclude(ru => ru.Rol)
                .FirstOrDefaultAsync(u => u.PersonId == personId && !u.IsDeleted);
        }
    }
}
