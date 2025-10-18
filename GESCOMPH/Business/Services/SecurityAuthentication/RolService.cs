using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Repository;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Rol;
using MapsterMapper;
using System.Linq.Expressions;

namespace Business.Services.SecurityAuthentication
{
    /// <summary>
    /// Servicio encargado de la gestión de roles dentro del sistema.
    /// Permite realizar operaciones CRUD, aplicar filtros personalizados,
    /// y definir reglas de unicidad y ordenamiento.
    /// </summary>
    public class RolService(
        IDataGeneric<Rol> rolRepository,
        IMapper mapper)
        : BusinessGeneric<RolSelectDto, RolCreateDto, RolUpdateDto, Rol>(rolRepository, mapper),
          IRolService
    {
        /// <summary>
        /// Define la regla de unicidad de negocio para la entidad <see cref="Rol"/>.
        /// En este caso, el nombre del rol debe ser único en el sistema.
        /// </summary>
        /// <param name="query">Consulta base de roles.</param>
        /// <param name="candidate">Entidad candidata a validar.</param>
        /// <returns>Consulta filtrada para verificar duplicados.</returns>
        protected override IQueryable<Rol>? ApplyUniquenessFilter(IQueryable<Rol> query, Rol candidate)
            => query.Where(r => r.Name == candidate.Name);

        /// <summary>
        /// Define los campos de texto sobre los que se puede realizar búsqueda libre.
        /// </summary>
        /// <returns>Expresiones con los campos buscables.</returns>
        protected override Expression<Func<Rol, string?>>[] SearchableFields() =>
        [
            r => r.Name
        ];

        /// <summary>
        /// Define los campos que pueden utilizarse para ordenamiento dinámico
        /// en consultas de roles.
        /// </summary>
        /// <returns>Lista de nombres de campos ordenables.</returns>
        protected override string[] SortableFields() =>
        [
            nameof(Rol.Name),
            nameof(Rol.Id),
            nameof(Rol.CreatedAt),
            nameof(Rol.Active)
        ];

        /// <summary>
        /// Define los filtros personalizados que pueden aplicarse
        /// en consultas de roles mediante parámetros de búsqueda.
        /// </summary>
        /// <returns>Diccionario de filtros permitidos y sus expresiones.</returns>
        protected override IDictionary<string, Func<string, Expression<Func<Rol, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<Rol, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Rol.Name)] = value => r => r.Name == value,
                [nameof(Rol.Active)] = value => r => r.Active == bool.Parse(value)
            };
    }
}
