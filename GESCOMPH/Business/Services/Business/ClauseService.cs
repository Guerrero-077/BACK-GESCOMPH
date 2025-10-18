using Business.Interfaces.Implements.Business;
using Business.Repository;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Clause;
using MapsterMapper;
using System.Linq.Expressions;

namespace Business.Services.Business
{
    public class ClauseService : BusinessGeneric<ClauseSelectDto, ClauseDto, ClauseUpdateDto, Clause>, IClauseService
    {
        public ClauseService(IDataGeneric<Clause> data, IMapper mapper) : base(data, mapper)
        {
        }

        /// <summary>
        /// Aplica un filtro de unicidad basado en la descripción de la cláusula.
        /// </summary>
        /// <param name="query">Consulta base sobre la entidad Clause.</param>
        /// <param name="candidate">Entidad candidata que se desea validar.</param>
        /// <returns>Consulta filtrada según la descripción única.</returns>
        protected override IQueryable<Clause>? ApplyUniquenessFilter(IQueryable<Clause> query, Clause candidate)
            => query.Where(c => c.Description == candidate.Description);

        /// <summary>
        /// Define los campos de la entidad que son buscables mediante operaciones de texto.
        /// </summary>
        /// <returns>Arreglo de expresiones con los campos buscables.</returns>
        protected override Expression<Func<Clause, string>>[] SearchableFields() =>
            [
                c => c.Name!,
                c => c.Description!
            ];

        /// <summary>
        /// Define los filtros permitidos para la consulta de cláusulas.
        /// </summary>
        /// <returns>Diccionario con los filtros y sus expresiones asociadas.</returns>
        protected override IDictionary<string, Func<string, Expression<Func<Clause, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<Clause, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Clause.Description)] = value => entity => entity.Description == value,
                [nameof(Clause.Active)] = value => entity => entity.Active == bool.Parse(value)
            };

        /// <summary>
        /// Define los campos que se pueden usar para ordenar las consultas de cláusulas.
        /// </summary>
        /// <returns>Arreglo con los nombres de los campos ordenables.</returns>
        protected override string[] SortableFields() =>
            [
                nameof(Clause.Description),
                nameof(Clause.Id),
                nameof(Clause.CreatedAt),
                nameof(Clause.Active)
            ];
    }
}
