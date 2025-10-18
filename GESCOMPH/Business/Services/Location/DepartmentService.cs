using Business.Interfaces.Implements.Location;
using Business.Repository;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.Location;
using Entity.DTOs.Implements.Location.Department;
using MapsterMapper;
using System.Linq.Expressions;

namespace Business.Services.Location
{
    /// <summary>
    /// Servicio de negocio encargado de gestionar las operaciones relacionadas con los departamentos.
    /// </summary>
    public class DepartmentService : BusinessGeneric<DepartmentSelectDto, DepartmentCreateDto, DepartmentUpdateDto, Department>, IDepartmentService
    {
        /// <summary>
        /// Inicializa una nueva instancia del servicio de departamentos.
        /// </summary>
        /// <param name="data">Repositorio genérico base.</param>
        /// <param name="mapper">Mapper para la conversión entre entidades y DTOs.</param>
        public DepartmentService(IDataGeneric<Department> data, IMapper mapper)
            : base(data, mapper)
        {
        }

        /// <summary>
        /// Define la clave única de negocio para evitar duplicados:
        /// un departamento con el mismo nombre no puede repetirse.
        /// </summary>
        protected override IQueryable<Department>? ApplyUniquenessFilter(IQueryable<Department> query, Department candidate)
            => query.Where(d => d.Name == candidate.Name);

        /// <summary>
        /// Define los campos que pueden ser buscados mediante texto.
        /// </summary>
        protected override Expression<Func<Department, string>>[] SearchableFields() =>
        [
            d => d.Name
        ];

        /// <summary>
        /// Define los campos que admiten ordenamiento dinámico.
        /// </summary>
        protected override string[] SortableFields() => new[]
        {
            nameof(Department.Name),
            nameof(Department.Id),
            nameof(Department.CreatedAt),
            nameof(Department.Active)
        };

        /// <summary>
        /// Define los filtros permitidos que pueden aplicarse desde los parámetros de consulta (query params).
        /// </summary>
        protected override IDictionary<string, Func<string, Expression<Func<Department, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<Department, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Department.Name)] = value => d => d.Name == value,
                [nameof(Department.Active)] = value => d => d.Active == bool.Parse(value)
            };
    }
}
