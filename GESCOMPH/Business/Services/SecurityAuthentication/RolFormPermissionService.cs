using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Repository;
using Business.Interfaces.Notifications;
using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.RolFormPemission;
using MapsterMapper;
using System.Linq.Expressions;

namespace Business.Services.SecurityAuthentication
{
    /// <summary>
    /// Servicio encargado de la gestión de permisos por formulario y rol.
    /// Controla la asignación, actualización y eliminación de permisos asociados
    /// a combinaciones de roles y formularios dentro del sistema.
    /// </summary>
    public class RolFormPermissionService
        : BusinessGeneric<RolFormPermissionSelectDto, RolFormPermissionCreateDto, RolFormPermissionUpdateDto, RolFormPermission>,
          IRolFormPermissionService
    {
        private readonly IRolFormPermissionRepository _repo;
        private readonly IUserContextService _auth;
        private readonly IPermissionsNotificationService _notify;

        public RolFormPermissionService(
            IRolFormPermissionRepository data,
            IMapper mapper,
            IUserContextService auth,
            IPermissionsNotificationService notify)
            : base(data, mapper)
        {
            _repo = data;
            _auth = auth;
            _notify = notify;
        }

        /// <summary>
        /// Define la regla de unicidad de negocio: una combinación Rol-Form-Permission debe ser única.
        /// </summary>
        protected override IQueryable<RolFormPermission>? ApplyUniquenessFilter(
            IQueryable<RolFormPermission> query, RolFormPermission candidate)
            => query.Where(rfp =>
                rfp.RolId == candidate.RolId &&
                rfp.FormId == candidate.FormId &&
                rfp.PermissionId == candidate.PermissionId);

        /// <summary>
        /// Crea un conjunto de permisos asociados a un rol y formulario.
        /// Valida duplicados antes de insertar y notifica los cambios de permisos.
        /// </summary>
        public override async Task<RolFormPermissionSelectDto> CreateAsync(RolFormPermissionCreateDto dto)
        {
            await ValidateDuplicatesAsync(dto.RolId, dto.FormId, dto.PermissionIds);

            var createdEntities = new List<RolFormPermission>();

            foreach (var permissionId in dto.PermissionIds)
            {
                var entity = new RolFormPermission
                {
                    RolId = dto.RolId,
                    FormId = dto.FormId,
                    PermissionId = permissionId,
                    Active = true
                };

                await _repo.AddAsync(entity);
                createdEntities.Add(entity);
            }

            var affectedUsers = await InvalidateUsersByRole(dto.RolId);
            await _notify.NotifyPermissionsUpdated(affectedUsers);

            if (createdEntities.Count == 0)
                return null;

            var entityToReturn = await _repo.GetByIdAsync(createdEntities.Last().Id);
            return _mapper.Map<RolFormPermissionSelectDto>(entityToReturn);
        }

        /// <summary>
        /// Actualiza los permisos asociados a un rol y formulario, añadiendo, eliminando
        /// o manteniendo los registros según corresponda. Notifica los cambios de permisos.
        /// </summary>
        public override async Task<RolFormPermissionSelectDto?> UpdateAsync(RolFormPermissionUpdateDto dto)
        {
            var group = (await _repo.GetByRolAndFormAsync(dto.RolId, dto.FormId)).ToList();

            var existingPermissionIds = group.Select(g => g.PermissionId).ToHashSet();
            var incomingPermissionIds = dto.PermissionIds.Distinct().ToHashSet();

            var toAdd = incomingPermissionIds.Except(existingPermissionIds).ToList();
            var toRemove = group.Where(g => !incomingPermissionIds.Contains(g.PermissionId)).ToList();

            if (toAdd.Any())
                await ValidateDuplicatesAsync(dto.RolId, dto.FormId, toAdd);

            foreach (var item in toRemove)
                await _repo.DeleteAsync(item.Id);

            foreach (var pid in toAdd)
            {
                var entity = new RolFormPermission
                {
                    RolId = dto.RolId,
                    FormId = dto.FormId,
                    PermissionId = pid,
                    Active = true
                };
                await _repo.AddAsync(entity);
                group.Add(entity);
            }

            var affectedUsers = await InvalidateUsersByRole(dto.RolId);
            await _notify.NotifyPermissionsUpdated(affectedUsers);

            if (group.Count == 0)
                return null;

            return new RolFormPermissionSelectDto
            {
                Id = group.First().Id,
                RolId = group.First().RolId,
                RolName = group.First().Rol?.Name ?? "",
                FormId = group.First().FormId,
                FormName = group.First().Form?.Name ?? "",
                Permissions = group
                    .Select(g => new PermissionInfoDto
                    {
                        PermissionId = g.PermissionId,
                        PermissionName = g.Permission?.Name ?? ""
                    })
                    .ToList(),
                Active = group.Any(g => g.Active)
            };
        }

        /// <summary>
        /// Obtiene todos los permisos agrupados por combinación de rol y formulario.
        /// </summary>
        public async Task<IEnumerable<RolFormPermissionSelectDto>> GetAllGroupedAsync()
        {
            var all = await _repo.GetAllAsync();

            return all
                .GroupBy(x => new { x.RolId, x.FormId })
                .Select(g =>
                {
                    var first = g.First();
                    return new RolFormPermissionSelectDto
                    {
                        Id = first.Id,
                        RolId = g.Key.RolId,
                        RolName = first.Rol?.Name ?? "",
                        FormId = g.Key.FormId,
                        FormName = first.Form?.Name ?? "",
                        Permissions = g.Select(p => new PermissionInfoDto
                        {
                            PermissionId = p.PermissionId,
                            PermissionName = p.Permission?.Name ?? ""
                        }).ToList(),
                        Active = g.Any(p => p.Active)
                    };
                });
        }

        /// <summary>
        /// Elimina todos los permisos asociados a un rol y formulario específico.
        /// </summary>
        public async Task<bool> DeleteByGroupAsync(int rolId, int formId)
        {
            var records = await _repo.GetByRolAndFormAsync(rolId, formId);
            if (!records.Any()) return false;

            foreach (var record in records)
                await _repo.DeleteAsync(record.Id);

            var affectedUsers = await InvalidateUsersByRole(rolId);
            await _notify.NotifyPermissionsUpdated(affectedUsers);
            return true;
        }

        /// <summary>
        /// Elimina un permiso individual y notifica los cambios a los usuarios afectados.
        /// </summary>
        public override async Task<bool> DeleteAsync(int id)
        {
            var rfp = await _repo.GetByIdAsync(id);
            var result = await base.DeleteAsync(id);

            if (result && rfp != null)
            {
                var affectedUsers = await InvalidateUsersByRole(rfp.RolId);
                await _notify.NotifyPermissionsUpdated(affectedUsers);
            }

            return result;
        }

        /// <summary>
        /// Actualiza el estado activo/inactivo de todos los permisos dentro de un grupo
        /// (rol + formulario) y notifica a los usuarios impactados.
        /// </summary>
        public override async Task<RolFormPermissionSelectDto> UpdateActiveStatusAsync(int id, bool active)
        {
            var target = await _repo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"No se encontró el permiso con ID {id}");

            var group = await _repo.GetByRolAndFormAsync(target.RolId, target.FormId);

            foreach (var item in group)
            {
                if (item.Active != active)
                {
                    item.Active = active;
                    await _repo.UpdateAsync(item);
                }
            }

            var affectedUsers = await InvalidateUsersByRole(target.RolId);
            await _notify.NotifyPermissionsUpdated(affectedUsers);

            var refreshed = await _repo.GetByIdAsync(id);
            return _mapper.Map<RolFormPermissionSelectDto>(refreshed);
        }

        /// <summary>
        /// Invalida el caché de los usuarios asociados a un rol,
        /// normalmente tras un cambio de permisos.
        /// </summary>
        private async Task<IReadOnlyList<int>> InvalidateUsersByRole(int rolId)
        {
            var userIds = await _repo.GetUserIdsByRoleIdAsync(rolId);
            foreach (var uid in userIds)
                _auth.InvalidateCache(uid);
            return userIds.ToList();
        }

        /// <summary>
        /// Valida que no existan duplicados antes de crear nuevas relaciones Rol-Form-Permission.
        /// </summary>
        private async Task ValidateDuplicatesAsync(int rolId, int formId, IEnumerable<int> permissionIds)
        {
            var existing = await _repo.GetByRolAndFormAsync(rolId, formId);

            foreach (var pid in permissionIds)
            {
                if (existing.Any(p => p.PermissionId == pid))
                    throw new InvalidOperationException($"El permiso con ID {pid} ya existe en este formulario.");
            }
        }

        /// <summary>
        /// Define los campos sobre los que se puede realizar búsqueda textual.
        /// </summary>
        protected override Expression<Func<RolFormPermission, string?>>[] SearchableFields() =>
        [
            x => x.Rol.Name,
            x => x.Form.Name,
            x => x.Permission.Name
        ];

        /// <summary>
        /// Define los campos que se pueden usar para ordenamiento dinámico.
        /// </summary>
        protected override string[] SortableFields() =>
        [
            nameof(RolFormPermission.Id),
            nameof(RolFormPermission.RolId),
            nameof(RolFormPermission.FormId),
            nameof(RolFormPermission.PermissionId),
            nameof(RolFormPermission.CreatedAt),
            nameof(RolFormPermission.Active)
        ];

        /// <summary>
        /// Define los filtros permitidos para consultas parametrizadas.
        /// </summary>
        protected override IDictionary<string, Func<string, Expression<Func<RolFormPermission, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<RolFormPermission, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(RolFormPermission.RolId)] = val => x => x.RolId == int.Parse(val),
                [nameof(RolFormPermission.FormId)] = val => x => x.FormId == int.Parse(val),
                [nameof(RolFormPermission.PermissionId)] = val => x => x.PermissionId == int.Parse(val),
                [nameof(RolFormPermission.Active)] = val => x => x.Active == bool.Parse(val)
            };
    }
}
