using Business.Interfaces.Implements.Business;
using Business.Repository;
using Data.Interfaz.DataBasic;
using Data.Interfaz.IDataImplement.Business;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Plaza;
using MapsterMapper;
using System.Linq.Expressions;
using Utilities.Exceptions;

namespace Business.Services.Business
{
    /// <summary>
    /// Servicio de negocio encargado de la gestión de las plazas,
    /// incluyendo validaciones de contratos y propagación de estados a establecimientos.
    /// </summary>
    public class PlazasService : BusinessGeneric<PlazaSelectDto, PlazaCreateDto, PlazaUpdateDto, Plaza>, IPlazaService
    {
        private readonly IDataGeneric<Plaza> _data;
        private readonly IEstablishmentsRepository _establishmentsRepository;
        private readonly IContractRepository _contractRepository;

        /// <summary>
        /// Inicializa una nueva instancia del servicio de plazas.
        /// </summary>
        /// <param name="data">Repositorio genérico de acceso a datos.</param>
        /// <param name="mapper">Mapper para conversión entre entidades y DTOs.</param>
        /// <param name="establishmentsRepository">Repositorio de establecimientos asociados.</param>
        /// <param name="contractRepository">Repositorio de contratos asociados.</param>
        public PlazasService(
            IDataGeneric<Plaza> data,
            IMapper mapper,
            IEstablishmentsRepository establishmentsRepository,
            IContractRepository contractRepository
        ) : base(data, mapper)
        {
            _data = data;
            _establishmentsRepository = establishmentsRepository;
            _contractRepository = contractRepository;
        }

        /// <summary>
        /// Define la clave única de negocio: 
        /// el nombre de la plaza no puede repetirse.
        /// </summary>
        protected override IQueryable<Plaza>? ApplyUniquenessFilter(IQueryable<Plaza> query, Plaza candidate)
            => query.Where(p => p.Name == candidate.Name);

        /// <summary>
        /// Actualiza el estado de activación de una plaza y propaga el cambio
        /// a los establecimientos asociados. Valida además que no existan contratos activos
        /// antes de desactivar la plaza.
        /// </summary>
        /// <param name="id">Identificador de la plaza.</param>
        /// <param name="active">Nuevo estado activo/inactivo.</param>
        /// <exception cref="BusinessException">Si existen contratos activos asociados.</exception>
        /// <exception cref="KeyNotFoundException">Si la plaza no existe.</exception>
        public override async Task UpdateActiveStatusAsync(int id, bool active)
        {
            var entity = await _data.GetByIdAsync(id)
                         ?? throw new KeyNotFoundException($"No se encontró el registro con ID {id}.");

            if (entity.Active == active) return;

            if (!active)
            {
                var hasActiveContracts = await _contractRepository.AnyActiveByPlazaAsync(id);
                if (hasActiveContracts)
                    throw new BusinessException("No se puede desactivar la plaza porque tiene establecimientos con contratos activos.");
            }

            entity.Active = active;
            await _data.UpdateAsync(entity);

            // Cascada: aplicar mismo estado a establecimientos asociados
            await _establishmentsRepository.SetActiveByPlazaIdAsync(id, active);
        }

        /// <summary>
        /// Define los campos que pueden ser buscados mediante texto.
        /// </summary>
        protected override Expression<Func<Plaza, string>>[] SearchableFields() =>
        [
            p => p.Name,
            p => p.Description,
            p => p.Location
        ];

        /// <summary>
        /// Define los campos que admiten ordenamiento dinámico.
        /// </summary>
        protected override string[] SortableFields() =>
        [
            nameof(Plaza.Name),
            nameof(Plaza.Description),
            nameof(Plaza.Location),
            nameof(Plaza.Active),
            nameof(Plaza.CreatedAt),
            nameof(Plaza.Id)
        ];

        /// <summary>
        /// Define los filtros permitidos para consultas mediante query params.
        /// </summary>
        protected override IDictionary<string, Func<string, Expression<Func<Plaza, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<Plaza, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Plaza.Name)] = value => p => p.Name == value,
                [nameof(Plaza.Active)] = value => p => p.Active == bool.Parse(value)
            };
    }
}
