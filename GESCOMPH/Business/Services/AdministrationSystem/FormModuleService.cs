using Business.Interfaces.Implements.AdministrationSystem;
using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Repository;
using Data.Interfaz.IDataImplement.AdministrationSystem;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.DTOs.Implements.AdministrationSystem.FormModule;
using MapsterMapper;
using System.Linq.Expressions;

namespace Business.Services.AdministrationSystem
{
    /// <summary>
    /// Servicio de negocio para la gestión de la relación entre formularios y módulos del sistema.
    /// Implementa reglas de unicidad, filtrado, ordenamiento e invalidación de caché por cambios en permisos.
    /// </summary>
    public class FormModuleService
        : BusinessGeneric<FormModuleSelectDto, FormModuleCreateDto, FormModuleUpdateDto, FormModule>,
          IFormMouduleService
    {
        private readonly IFormModuleRepository _repo;
        private readonly IUserContextService _auth;

        /// <summary>
        /// Inicializa una nueva instancia del servicio de gestión de vínculos entre formularios y módulos.
        /// </summary>
        /// <param name="data">Repositorio genérico de datos para <see cref="FormModule"/>.</param>
        /// <param name="mapper">Mapper de entidades y DTOs.</param>
        /// <param name="auth">Servicio de contexto de usuario para invalidar caché de permisos.</param>
        public FormModuleService(IFormModuleRepository data, IMapper mapper, IUserContextService auth)
            : base(data, mapper)
        {
            _repo = data;
            _auth = auth;
        }

        /// <summary>
        /// Define la regla de unicidad de negocio: no se puede repetir la combinación FormId + ModuleId.
        /// </summary>
        protected override IQueryable<FormModule>? ApplyUniquenessFilter(IQueryable<FormModule> query, FormModule candidate)
            => query.Where(fm => fm.FormId == candidate.FormId && fm.ModuleId == candidate.ModuleId);

        /// <summary>
        /// Crea un vínculo entre un formulario y un módulo.
        /// Invalida la caché de usuarios que tengan permisos sobre ese formulario.
        /// </summary>
        public override async Task<FormModuleSelectDto> CreateAsync(FormModuleCreateDto dto)
        {
            var result = await base.CreateAsync(dto);

            var userIds = await _repo.GetUserIdsByFormIdAsync(dto.FormId);
            foreach (var uid in userIds)
                _auth.InvalidateCache(uid);

            return result;
        }

        /// <summary>
        /// Actualiza un vínculo existente y limpia la caché de usuarios afectados.
        /// </summary>
        public override async Task<FormModuleSelectDto> UpdateAsync(FormModuleUpdateDto dto)
        {
            var result = await base.UpdateAsync(dto);

            var userIds = await _repo.GetUserIdsByFormIdAsync(dto.FormId);
            foreach (var uid in userIds)
                _auth.InvalidateCache(uid);

            return result;
        }

        /// <summary>
        /// Elimina un vínculo entre formulario y módulo y actualiza la caché de permisos.
        /// </summary>
        public override async Task<bool> DeleteAsync(int id)
        {
            var fm = await _repo.GetByIdAsync(id);
            var deleted = await base.DeleteAsync(id);

            if (deleted && fm is not null)
            {
                var userIds = await _repo.GetUserIdsByFormIdAsync(fm.FormId);
                foreach (var uid in userIds)
                    _auth.InvalidateCache(uid);
            }

            return deleted;
        }

        /// <summary>
        /// Campos habilitados para búsquedas parciales o exactas.
        /// </summary>
        protected override Expression<Func<FormModule, string>>[] SearchableFields() =>
        [
            f => f.Form.Name!,
            f => f.Module.Name!
        ];

        /// <summary>
        /// Campos habilitados para ordenamiento dinámico en consultas.
        /// </summary>
        protected override string[] SortableFields() =>
        [
            nameof(FormModule.FormId),
            nameof(FormModule.ModuleId),
            nameof(FormModule.Id),
            nameof(FormModule.CreatedAt),
            nameof(FormModule.Active)
        ];

        /// <summary>
        /// Define los filtros explícitamente permitidos por query parameters.
        /// </summary>
        protected override IDictionary<string, Func<string, Expression<Func<FormModule, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<FormModule, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(FormModule.FormId)] = value => entity => entity.FormId == int.Parse(value),
                [nameof(FormModule.ModuleId)] = value => entity => entity.ModuleId == int.Parse(value),
                [nameof(FormModule.Active)] = value => entity => entity.Active == bool.Parse(value)
            };
    }
}
