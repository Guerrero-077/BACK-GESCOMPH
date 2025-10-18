using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Repository;
using Data.Interfaz.IDataImplement.Persons;
using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Entity.Domain.Models.Implements.Persons;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.User;
using Entity.Infrastructure.Context;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Utilities.Exceptions;
using Utilities.Helpers.GeneratePassword;
using Utilities.Messaging.Interfaces;

namespace Business.Services.SecurityAuthentication
{
    public class UserService
        : BusinessGeneric<UserSelectDto, UserCreateDto, UserUpdateDto, User>, IUserService
    {
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IUserRepository _userRepository;
        private readonly IRolUserRepository _rolUserRepository;
        private readonly IPersonRepository _personRepository;
        private readonly ApplicationDbContext _context;
        private readonly ISendCode _emailService;

        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            IPasswordHasher<User> passwordHasher,
            IRolUserRepository rolUserRepository,
            IPersonRepository personRepository,
            ISendCode emailService,
            ApplicationDbContext context
        ) : base(userRepository, mapper)
        {
            _passwordHasher = passwordHasher;
            _userRepository = userRepository;
            _rolUserRepository = rolUserRepository;
            _personRepository = personRepository;
            _context = context;
            _emailService = emailService;
        }

        // ======================================================
        // =================== MÉTODOS DE LECTURA ===============
        // ======================================================

        /// <summary>
        /// Obtiene todos los usuarios con sus roles asociados.
        /// </summary>
        public override async Task<IEnumerable<UserSelectDto>> GetAllAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var result = new List<UserSelectDto>(capacity: users.Count());

            foreach (var u in users)
            {
                var dto = _mapper.Map<UserSelectDto>(u);
                dto.Roles = await _rolUserRepository.GetRoleNamesByUserIdAsync(u.Id);
                result.Add(dto);
            }

            return result;
        }

        // ======================================================
        // ================== CREACIÓN COMPLETA =================
        // ======================================================

        /// <summary>
        /// Crea un nuevo usuario junto con su persona y roles asociados.
        /// Genera una contraseña temporal y la envía por correo.
        /// </summary>
        /// <param name="dto">Datos de creación del usuario.</param>
        public override async Task<UserSelectDto> CreateAsync(UserCreateDto dto)
        {


            if (await _userRepository.ExistsByEmailAsync(dto.Email))
                throw new BusinessException("El correo ya está registrado.");

            if (await _personRepository.ExistsByDocumentAsync(dto.Document))
                throw new BusinessException("Ya existe una persona con este número de documento.");

            var strategy = _context.Database.CreateExecutionStrategy();
            string? tempPassword = null;

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                var person = _mapper.Map<Person>(dto);
                var user = _mapper.Map<User>(dto);
                user.Person = person;

                tempPassword = PasswordGenerator.Generate(12);
                user.Password = _passwordHasher.HashPassword(user, tempPassword);

                var mustChangeProp = typeof(User).GetProperty("MustChangePassword");
                mustChangeProp?.SetValue(user, true);

                await _userRepository.AddAsync(user);

                var roleIds = (dto.RoleIds ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList();
                await _rolUserRepository.ReplaceUserRolesAsync(user.Id, roleIds);

                await tx.CommitAsync();
            });

            await SendTemporaryPasswordAsync(dto.Email, $"{dto.FirstName} {dto.LastName}".Trim(), tempPassword!);

            var userId = await _userRepository.GetIdByEmailAsync(dto.Email)
                         ?? throw new Exception("No se pudo recuperar el ID del usuario tras el registro.");

            var created = await _userRepository.GetByIdWithDetailsAsync(userId)
                          ?? throw new Exception("No se pudo recuperar el usuario tras registrarlo.");

            var result = _mapper.Map<UserSelectDto>(created);
            result.Roles = (await _rolUserRepository.GetRoleNamesByUserIdAsync(created.Id)).ToList();
            return result;
        }

        // ======================================================
        // =================== ACTUALIZACIÓN ====================
        // ======================================================

        /// <summary>
        /// Actualiza un usuario junto con sus datos personales y roles asociados.
        /// </summary>
        /// <param name="dto">Datos de actualización del usuario.</param>
        public override async Task<UserSelectDto> UpdateAsync(UserUpdateDto dto)
        {

            var user = await _userRepository.GetByIdForUpdateAsync(dto.Id)
                       ?? throw new BusinessException("Usuario no encontrado.");

            if (await _userRepository.ExistsByEmailExcludingIdAsync(dto.Id, dto.Email))
                throw new BusinessException("El correo ya está registrado por otro usuario.");

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                _mapper.Map(dto, user);

                if (user.Person is null)
                    throw new BusinessException("El usuario no tiene una persona asociada.");

                _mapper.Map(dto, user.Person);

                _context.Entry(user.Person).Property(p => p.Document).IsModified = false;

                await _userRepository.UpdateAsync(user);

                var roleIds = (dto.RoleIds ?? Array.Empty<int>()).Where(x => x > 0).Distinct().ToList();
                await _rolUserRepository.ReplaceUserRolesAsync(user.Id, roleIds);

                await tx.CommitAsync();
            });

            var updated = await _userRepository.GetByIdWithDetailsAsync(user.Id)
                          ?? throw new Exception("No se pudo recuperar el usuario actualizado.");

            var result = _mapper.Map<UserSelectDto>(updated);
            result.Roles = (await _rolUserRepository.GetRoleNamesByUserIdAsync(updated.Id)).ToList();
            return result;
        }

        // ======================================================
        // =========== CREACIÓN CON PERSONA EXISTENTE ===========
        // ======================================================

        /// <summary>
        /// Garantiza que exista un usuario asociado a una persona.
        /// Si no existe, lo crea con una contraseña temporal.
        /// </summary>
        /// <param name="personId">Identificador de la persona.</param>
        /// <param name="email">Correo electrónico del usuario.</param>
        public async Task<(int userId, bool created, string? tempPassword)> EnsureUserForPersonAsync(int personId, string email)
        {
            if (personId <= 0)
                throw new BusinessException("PersonId inválido.");

            if (string.IsNullOrWhiteSpace(email))
                throw new BusinessException("El correo es requerido.");

            var normalizedEmail = email.Trim();
            var existing = await _userRepository.GetByPersonIdAsync(personId);
            if (existing is not null)
                return (existing.Id, false, null);

            if (await _userRepository.ExistsByEmailAsync(normalizedEmail))
                throw new BusinessException("El correo ya está registrado.");

            if (await _personRepository.GetByIdAsync(personId) is null)
                throw new BusinessException("Persona no encontrada para crear el usuario.");

            var tempPassword = PasswordGenerator.Generate(12);
            var user = new User
            {
                Email = normalizedEmail,
                PersonId = personId,
                Password = _passwordHasher.HashPassword(new User(), tempPassword)
            };

            await _userRepository.AddAsync(user);
            await _rolUserRepository.AsignateRolDefault(user);

            return (user.Id, true, tempPassword);
        }

        // ======================================================
        // =============== MÉTODOS DE SOPORTE ===================
        // ======================================================

        /// <summary>
        /// Envía la contraseña temporal por correo electrónico.
        /// </summary>
        private async Task SendTemporaryPasswordAsync(string email, string fullName, string tempPassword)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(tempPassword))
                    await _emailService.SendTemporaryPasswordAsync(email, fullName, tempPassword);
            }
            catch
            {
                // No se interrumpe el flujo principal si el envío de correo falla.
            }
        }

        /// <summary>
        /// Define los campos habilitados para búsqueda textual.
        /// </summary>
        protected override Expression<Func<User, string?>>[] SearchableFields() =>
        [
            x => x.Email,
            x => x.Person.FirstName,
            x => x.Person.LastName,
            x => x.Person.Document,
            x => x.Person.Phone,
            x => x.Person.Address,
            x => x.Person.City.Name
        ];

        /// <summary>
        /// Define los campos permitidos para ordenamiento.
        /// </summary>
        protected override string[] SortableFields() => new[]
        {
            nameof(User.Email),
            "Person.Document",
            "Person.Phone",
            "Person.Address",
            "Person.City.Name",
            nameof(User.Active),
            nameof(User.CreatedAt),
            nameof(User.Id)
        };

        /// <summary>
        /// Define el mapeo de campos ordenables para expresiones de LINQ dinámico.
        /// </summary>
        protected override IDictionary<string, LambdaExpression> SortMap()
            => new Dictionary<string, LambdaExpression>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(User.Email)] = (Expression<Func<User, string>>)(u => u.Email),
                ["Person.Document"] = (Expression<Func<User, string?>>)(u => u.Person.Document),
                ["Person.Phone"] = (Expression<Func<User, string?>>)(u => u.Person.Phone),
                ["Person.Address"] = (Expression<Func<User, string?>>)(u => u.Person.Address),
                ["Person.City.Name"] = (Expression<Func<User, string>>)(u => u.Person.City.Name),
                [nameof(User.Active)] = (Expression<Func<User, bool>>)(u => u.Active),
                [nameof(User.CreatedAt)] = (Expression<Func<User, DateTime>>)(u => u.CreatedAt),
                [nameof(User.Id)] = (Expression<Func<User, int>>)(u => u.Id),
            };

        /// <summary>
        /// Define los filtros disponibles en consultas dinámicas.
        /// </summary>
        protected override IDictionary<string, Func<string, Expression<Func<User, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<User, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(User.Email)] = value => x => x.Email == value,
                [nameof(User.PersonId)] = value => x => x.PersonId == int.Parse(value),
                [nameof(User.Active)] = value => x => x.Active == bool.Parse(value),
                [nameof(User.Id)] = value => x => x.Id == int.Parse(value)
            };
    }
}
