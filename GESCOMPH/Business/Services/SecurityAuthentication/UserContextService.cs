using Business.Interfaces.Implements.SecurityAuthentication;
using Data.Interfaz.Security;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Me;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Utilities.Exceptions;

namespace Business.Services.SecurityAuthentication
{
    /// <summary>
    /// Servicio encargado de construir y mantener en caché el contexto de usuario (/me),
    /// incluyendo roles, formularios, módulos y permisos asociados.
    /// </summary>
    public class UserContextService : IUserContextService
    {
        private readonly IUserMeRepository _repo;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(10);

        private static string Key(int userId) => $"UserContext:{userId}";

        /// <summary>
        /// Inicializa una nueva instancia del servicio de contexto de usuario.
        /// </summary>
        /// <param name="repo">Repositorio para obtener el contexto completo del usuario.</param>
        /// <param name="mapper">Instancia de <see cref="IMapper"/> (no utilizada directamente aquí, pero mantenida por consistencia DI).</param>
        /// <param name="cache">Caché en memoria para optimizar las consultas repetitivas.</param>
        public UserContextService(IUserMeRepository repo, IMapper mapper, IMemoryCache cache)
        {
            _repo = repo;
            _cache = cache;
        }

        /// <summary>
        /// Construye el contexto completo del usuario, incluyendo roles, permisos y menús,
        /// con almacenamiento temporal en caché para mejorar el rendimiento.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <returns>Un objeto <see cref="UserMeDto"/> con toda la información del contexto del usuario.</returns>
        /// <exception cref="BusinessException">Si el usuario no existe o fue eliminado.</exception>
        public async Task<UserMeDto> BuildUserContextAsync(int userId)
        {
            var cacheKey = Key(userId);
            if (_cache.TryGetValue<UserMeDto>(cacheKey, out var cached))
                return cached;

            var user = await _repo.GetUserWithFullContextAsync(userId)
                       ?? throw new BusinessException("Usuario no encontrado.");

            // === Roles activos y válidos ===
            var roles = user.RolUsers?
                .Select(ru => ru.Rol)
                .Where(r => r != null && r.Active && !r.IsDeleted)
                .DistinctBy(r => r!.Id)
                .Cast<Rol>()
                .ToList() ?? new();

            var roleNames = roles.Select(r => r.Name!).ToList();

            // === Formularios permitidos (por roles) ===
            var allowedForms = roles
                .SelectMany(r => r.RolFormPermissions ?? Enumerable.Empty<RolFormPermission>())
                .Where(rfp => rfp.Active && !rfp.IsDeleted)
                .Select(rfp => rfp.Form)
                .Where(f => f != null && f.Active && !f.IsDeleted)
                .DistinctBy(f => f!.Id)
                .Cast<Form>()
                .ToList();

            // === Construcción de módulos y menús ===
            var modules = allowedForms
                .SelectMany(f => f.FormModules ?? Enumerable.Empty<FormModule>())
                .Select(fm => fm.Module)
                .Where(m => m != null && m.Active && !m.IsDeleted)
                .DistinctBy(m => m!.Id)
                .Cast<Module>()
                .OrderBy(m => m.Name)
                .Select(module =>
                {
                    var moduleDto = module.Adapt<MenuModuleDto>();

                    var moduleForms = allowedForms
                        .Where(f => f.FormModules.Any(fm => fm.ModuleId == module.Id))
                        .OrderBy(f => f.Name)
                        .Select(f =>
                        {
                            var formDto = f.Adapt<FormDto>();

                            // Permisos del formulario, consolidados entre roles
                            var formPerms = roles
                                .SelectMany(r => r.RolFormPermissions ?? Enumerable.Empty<RolFormPermission>())
                                .Where(rfp => rfp.Active && !rfp.IsDeleted && rfp.FormId == f.Id && rfp.Permission != null)
                                .Select(rfp => NormalizePermission(rfp.Permission!.Name!))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            formDto.Permissions = formPerms;
                            return formDto;
                        })
                        .ToList();

                    moduleDto.Forms = moduleForms;
                    return moduleDto;
                })
                .ToList();

            // === Construcción del DTO final ===
            var dto = new UserMeDto
            {
                Id = user.Id,
                PersonId = user.PersonId,
                FullName = string.Join(" ",
                             new[] { user.Person?.FirstName, user.Person?.LastName }
                             .Where(s => !string.IsNullOrWhiteSpace(s))).Trim(),
                Email = user.Email,
                Roles = roleNames,
                Menu = modules
            };

            _cache.Set(cacheKey, dto, _cacheTtl);
            return dto;
        }

        /// <summary>
        /// Invalida la caché del contexto de un usuario específico,
        /// forzando su reconstrucción en la siguiente consulta.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        public void InvalidateCache(int userId) => _cache.Remove(Key(userId));

        /// <summary>
        /// Normaliza el nombre de un permiso para su uso en comparaciones o UI.
        /// </summary>
        /// <param name="p">Nombre original del permiso.</param>
        /// <returns>Permiso en mayúsculas y sin espacios.</returns>
        private static string NormalizePermission(string p)
            => p.Trim().ToUpperInvariant().Replace(" ", "_");
    }
}
