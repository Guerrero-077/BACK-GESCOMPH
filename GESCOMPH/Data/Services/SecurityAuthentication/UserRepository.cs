using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Data.Repository;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.SecurityAuthentication
{
    public class UserRepository : DataGeneric<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context) { }

        // ======================================================
        // =============== MÉTODOS CRUD PRINCIPALES =============
        // ======================================================

        /// <summary>
        /// Obtiene todos los usuarios que no están eliminados, incluyendo sus relaciones con Persona, Ciudad y Roles.
        /// </summary>
        public override async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(u => !u.IsDeleted)
                .OrderByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.Id)
                .Include(u => u.Person)
                    .ThenInclude(p => p.City)
                .Include(u => u.RolUsers)
                    .ThenInclude(ru => ru.Rol)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene un usuario por su identificador único si no está eliminado.
        /// </summary>
        /// <param name="id">Id del usuario.</param>
        public override async Task<User?> GetByIdAsync(int id)
        {
            return await _dbSet
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        /// <summary>
        /// Obtiene un usuario por Id con tracking habilitado para operaciones de actualización.
        /// </summary>
        /// <param name="id">Id del usuario.</param>
        public async Task<User?> GetByIdForUpdateAsync(int id)
        {
            return await _dbSet
                .Include(u => u.Person)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        /// <summary>
        /// Obtiene un usuario por Id con todos los detalles relacionados (Persona, Ciudad, Roles).
        /// </summary>
        /// <param name="id">Id del usuario.</param>
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

        // ======================================================
        // ============ MÉTODOS DE CONSULTA Y VALIDACIÓN =========
        // ======================================================

        /// <summary>
        /// Verifica si existe un usuario activo con el email especificado.
        /// </summary>
        /// <param name="email">Correo electrónico del usuario.</param>
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            var e = email.Trim().ToLower();
            return await _dbSet.AnyAsync(u => !u.IsDeleted && u.Email.ToLower() == e);
        }

        /// <summary>
        /// Verifica si existe un usuario con el email especificado, excluyendo un Id determinado (útil en actualizaciones).
        /// </summary>
        /// <param name="id">Id del usuario a excluir.</param>
        /// <param name="email">Correo electrónico a verificar.</param>
        public async Task<bool> ExistsByEmailExcludingIdAsync(int id, string email)
        {
            var e = email.Trim().ToLower();
            return await _dbSet.AnyAsync(u => !u.IsDeleted && u.Id != id && u.Email.ToLower() == e);
        }

        /// <summary>
        /// Obtiene el Id de un usuario a partir de su correo electrónico.
        /// </summary>
        /// <param name="email">Correo electrónico del usuario.</param>
        public async Task<int?> GetIdByEmailAsync(string email)
        {
            var e = email.Trim().ToLower();
            return await _dbSet
                .Where(u => !u.IsDeleted && u.Email.ToLower() == e)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync();
        }

        // ======================================================
        // ============ MÉTODOS DE CONSULTA PERSONALIZADA =========
        // ======================================================

        /// <summary>
        /// Obtiene un usuario por correo electrónico incluyendo relaciones completas (Persona y Roles).
        /// </summary>
        /// <param name="email">Correo electrónico del usuario.</param>
        public async Task<User?> GetByEmailAsync(string email)
        {
            var e = email.Trim().ToLower();
            return await _dbSet
                .Include(u => u.Person)
                .Include(u => u.RolUsers)
                    .ThenInclude(ru => ru.Rol)
                .FirstOrDefaultAsync(u => !u.IsDeleted && u.Email.ToLower() == e);
        }

        /// <summary>
        /// Obtiene un usuario por correo electrónico con una proyección de campos básicos.
        /// </summary>
        /// <param name="email">Correo electrónico del usuario.</param>
        public async Task<User?> GetByEmailProjectionAsync(string email)
        {
            var e = email.Trim().ToLower();
            return await _dbSet
                .AsNoTracking()
                .Where(u => !u.IsDeleted && u.Email.ToLower() == e)
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

        /// <summary>
        /// Obtiene un usuario por correo electrónico con datos mínimos necesarios para autenticación.
        /// </summary>
        /// <param name="email">Correo electrónico del usuario.</param>
        public async Task<User?> GetAuthUserByEmailAsync(string email)
        {
            var e = email.Trim().ToLower();
            return await _dbSet
                .AsNoTracking()
                .Where(u => !u.IsDeleted && u.Email.ToLower() == e)
                .Select(u => new User
                {
                    Id = u.Id,
                    Email = u.Email,
                    Password = u.Password,
                    PersonId = u.PersonId
                })
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Obtiene un usuario a partir del Id de la persona relacionada.
        /// </summary>
        /// <param name="personId">Id de la persona asociada al usuario.</param>
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
