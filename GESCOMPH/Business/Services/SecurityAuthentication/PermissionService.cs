using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Repository;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Permission;
using MapsterMapper;
using System.Linq.Expressions;

namespace Business.Services.SecurityAuthentication
{
    /// <summary>
    /// Servicio de gestión de permisos del sistema. 
    /// Hereda de <see cref="BusinessGeneric{TSelectDto, TCreateDto, TUpdateDto, TEntity}"/> 
    /// para proveer operaciones CRUD con lógica de negocio adicional.
    /// </summary>
    public class PermissionService : BusinessGeneric<PermissionSelectDto, PermissionCreateDto, PermissionUpdateDto, Permission>, IPermissionService
    {
        public PermissionService(IDataGeneric<Permission> data, IMapper mapper)
            : base(data, mapper) { }

        /// <summary>
        /// Define la condición de unicidad de negocio para la entidad Permission.
        /// Previene la creación o actualización de registros duplicados según el nombre del permiso.
        /// </summary>
        protected override IQueryable<Permission>? ApplyUniquenessFilter(IQueryable<Permission> query, Permission candidate)
            => query.Where(p => p.Name == candidate.Name);

        /// <summary>
        /// Define los campos sobre los cuales se permite la búsqueda textual (filtros de texto libre).
        /// </summary>
        protected override Expression<Func<Permission, string?>>[] SearchableFields() =>
        [
            p => p.Name,
            p => p.Description
        ];

        /// <summary>
        /// Define los campos permitidos para ordenamiento dinámico en las consultas.
        /// </summary>
        protected override string[] SortableFields() =>
        [
            nameof(Permission.Name),
            nameof(Permission.Description),
            nameof(Permission.Id),
            nameof(Permission.CreatedAt),
            nameof(Permission.Active)
        ];

        /// <summary>
        /// Define los filtros exactos permitidos en consultas parametrizadas.
        /// Las claves del diccionario representan el nombre del campo y las expresiones su condición.
        /// </summary>
        protected override IDictionary<string, Func<string, Expression<Func<Permission, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<Permission, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Permission.Name)] = value => p => p.Name == value,
                [nameof(Permission.Active)] = value => p => p.Active == bool.Parse(value)
            };
    }
}
