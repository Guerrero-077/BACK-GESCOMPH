using Business.Interfaces.Implements.Business;
using Business.Repository;
using Data.Interfaz.IDataImplement.Business;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.EstablishmentDto;
using Entity.Enum;
using Entity.Infrastructure.Context;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Linq.Expressions;
using Utilities.Exceptions;
using Utilities.Helpers.Business;

namespace Business.Services.Business
{
    /// <summary>
    /// Servicio de Establecimientos. Gestiona operaciones CRUD, validaciones de dominio
    /// y cálculos financieros dependientes de parámetros del sistema (UVT).
    /// </summary>
    public sealed class EstablishmentService :
        BusinessGeneric<EstablishmentSelectDto, EstablishmentCreateDto, EstablishmentUpdateDto, Establishment>,
        IEstablishmentService
    {
        private readonly IEstablishmentsRepository _repo;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EstablishmentService> _logger;
        private readonly IDataGeneric<SystemParameter> _systemParamRepository;

        public EstablishmentService(
            IEstablishmentsRepository repo,
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<EstablishmentService> logger,
            IDataGeneric<SystemParameter> systemParamRepository
        ) : base(repo, mapper)
        {
            _repo = repo;
            _context = context;
            _logger = logger;
            _systemParamRepository = systemParamRepository;
        }

        /// <summary>Define la clave de unicidad del establecimiento (Nombre).</summary>
        protected override IQueryable<Establishment>? ApplyUniquenessFilter(IQueryable<Establishment> query, Establishment candidate)
            => query.Where(e => e.Name == candidate.Name);

        // =====================================================================
        // CONSULTAS
        // =====================================================================

        /// <summary>Obtiene todos los establecimientos, activos e inactivos.</summary>
        public async Task<IReadOnlyList<EstablishmentSelectDto>> GetAllAnyAsync(int? limit = null)
        {
            var list = await _repo.GetAllAsync(ActivityFilter.Any, limit);
            return list.Select(e => e.Adapt<EstablishmentSelectDto>()).ToList().AsReadOnly();
        }

        /// <summary>Obtiene solo los establecimientos activos.</summary>
        public async Task<IReadOnlyList<EstablishmentSelectDto>> GetAllActiveAsync(int? limit = null)
        {
            var list = await _repo.GetAllAsync(ActivityFilter.ActiveOnly, limit);
            return list.Select(e => e.Adapt<EstablishmentSelectDto>()).ToList().AsReadOnly();
        }

        /// <summary>Obtiene establecimientos asociados a una plaza.</summary>
        public async Task<IReadOnlyList<EstablishmentSelectDto>> GetByPlazaIdAsync(int plazaId, bool activeOnly = false, int? limit = null)
        {
            if (plazaId <= 0)
                return Array.Empty<EstablishmentSelectDto>();

            var filter = activeOnly ? ActivityFilter.ActiveOnly : ActivityFilter.Any;
            var list = await _repo.GetByPlazaIdAsync(plazaId, filter, limit);
            return list.Select(e => e.Adapt<EstablishmentSelectDto>()).ToList().AsReadOnly();
        }

        /// <summary>Obtiene un establecimiento por Id (sin importar su estado).</summary>
        public async Task<EstablishmentSelectDto?> GetByIdAnyAsync(int id)
        {
            var e = await _repo.GetByIdAnyAsync(id);
            return e?.Adapt<EstablishmentSelectDto>();
        }

        /// <summary>Obtiene un establecimiento activo por Id.</summary>
        public async Task<EstablishmentSelectDto?> GetByIdActiveAsync(int id)
        {
            var e = await _repo.GetByIdActiveAsync(id);
            return e?.Adapt<EstablishmentSelectDto>();
        }

        // =====================================================================
        // CRUD
        // =====================================================================

        public override async Task<EstablishmentSelectDto> CreateAsync(EstablishmentCreateDto dto)
        {
            try
            {
                var entity = dto.Adapt<Establishment>();

                var uvtValue = await GetParameterValueAsync("UVT", DateTime.UtcNow);
                entity.RentValueBase = Math.Round(dto.UvtQty * uvtValue, 2, MidpointRounding.AwayFromZero);

                await _repo.AddAsync(entity);

                var created = await _repo.GetByIdAnyAsync(entity.Id) ?? entity;
                return created.Adapt<EstablishmentSelectDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear establecimiento.");
                throw new BusinessException("Error al crear el establecimiento.", ex);
            }
        }

        public override async Task<EstablishmentSelectDto?> UpdateAsync(EstablishmentUpdateDto dto)
        {
            try
            {
                var entity = await _repo.GetByIdAnyAsync(dto.Id)
                    ?? throw new BusinessException($"No existe el establecimiento con Id {dto.Id}.");
                
                dto.Adapt(entity);

                var uvtValue = await GetParameterValueAsync("UVT", DateTime.UtcNow);
                entity.RentValueBase = Math.Round(entity.UvtQty * uvtValue, 2, MidpointRounding.AwayFromZero);

                await _repo.UpdateAsync(entity);

                var reloaded = await _repo.GetByIdAnyAsync(entity.Id) ?? entity;
                return reloaded.Adapt<EstablishmentSelectDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar establecimiento {Id}.", dto.Id);
                throw new BusinessException("Error al actualizar el establecimiento.", ex);
            }
        }

        public override async Task<bool> DeleteAsync(int id)
        {
            try
            {
                BusinessValidationHelper.ThrowIfZeroOrLess(id, "El ID debe ser mayor que cero.");

                if (await _context.PremisesLeaseds.AnyAsync(p => p.EstablishmentId == id && !p.IsDeleted))
                    throw new BusinessException("No se puede eliminar un establecimiento que tiene contratos asociados.");

                var entity = await _repo.GetByIdAnyAsync(id);
                if (entity == null)
                    return false;

                return await _repo.DeleteAsync(id);
            }
            catch (DbUpdateException dbx)
            {
                throw new BusinessException($"No se pudo eliminar el registro con ID {id} por restricciones de datos.", dbx);
            }
            catch (Exception ex)
            {
                throw new BusinessException($"Error al eliminar el registro con ID {id}.", ex);
            }
        }

        // =====================================================================
        // PROYECCIONES Y OPERACIONES DE NEGOCIO
        // =====================================================================

        /// <summary>Obtiene una lista básica de establecimientos (Id, RentValue, etc.).</summary>
        public async Task<IReadOnlyList<EstablishmentBasicsDto>> GetBasicsByIdsAsync(IEnumerable<int> ids)
        {
            var distinct = ids?.Distinct().ToList() ?? new();
            if (distinct.Count == 0)
                return Array.Empty<EstablishmentBasicsDto>();

            var basics = await _repo.GetBasicsByIdsAsync(distinct);
            return basics.ToList().AsReadOnly();
        }

        /// <summary>
        /// Reserva establecimientos para un contrato (marcándolos como inactivos temporalmente).
        /// </summary>
        public async Task<(decimal totalBaseRent, decimal totalUvt)> ReserveForContractAsync(IReadOnlyCollection<int> ids)
        {
            var distinct = ids?.Where(id => id > 0).Distinct().ToList() ?? new();
            if (distinct.Count == 0)
                throw new BusinessException("Debe seleccionar al menos un establecimiento.");

            var inactive = await _repo.GetInactiveIdsAsync(distinct);
            if (inactive.Count > 0)
                throw new BusinessException($"Los establecimientos {string.Join(", ", inactive)} no están disponibles (Active = false).");

            var basics = await _repo.GetBasicsByIdsAsync(distinct);
            if (basics.Count != distinct.Count)
                throw new BusinessException("Conflicto de concurrencia al recuperar los establecimientos seleccionados.");

            var totalBase = basics.Sum(b => b.RentValueBase);
            var totalUvt = basics.Sum(b => b.UvtQty);

            var affected = await _repo.SetActiveByIdsAsync(distinct, active: false);
            if (affected != distinct.Count)
                throw new BusinessException("Conflicto de concurrencia al actualizar estados de establecimientos.");

            return (totalBase, totalUvt);
        }

        /// <summary>Obtiene establecimientos para mostrar en tarjetas (grid/cards).</summary>
        public async Task<IReadOnlyList<EstablishmentCardDto>> GetCardsAnyAsync()
        {
            var list = await _repo.GetCardsAsync(ActivityFilter.Any);
            return list.ToList().AsReadOnly();
        }

        /// <summary>Obtiene establecimientos activos para tarjetas (grid/cards).</summary>
        public async Task<IReadOnlyList<EstablishmentCardDto>> GetCardsActiveAsync()
        {
            var list = await _repo.GetCardsAsync(ActivityFilter.ActiveOnly);
            return list.ToList().AsReadOnly();
        }

        // Validaciones de campo y normalizaciones se gestionan mediante FluentValidation
        // en los validators de DTO correspondientes (Create/Update).

        // =====================================================================
        // CONFIGURACIÓN DE CONSULTAS (BusinessGeneric)
        // =====================================================================

        protected override Expression<Func<Establishment, string>>[] SearchableFields() =>
        [
            e => e.Name!,
            e => e.Description!,
            e => e.Address!,
            e => e.Plaza.Name!
        ];

        protected override string[] SortableFields() =>
        [
            nameof(Establishment.Name),
            nameof(Establishment.Description),
            nameof(Establishment.RentValueBase),
            nameof(Establishment.AreaM2),
            nameof(Establishment.PlazaId),
            nameof(Establishment.Id),
            nameof(Establishment.CreatedAt),
            nameof(Establishment.Active)
        ];

        // =====================================================================
        // PARÁMETROS DEL SISTEMA
        // =====================================================================

        private async Task<decimal> GetParameterValueAsync(string key, DateTime date)
        {
            var param = await _systemParamRepository.GetAllQueryable()
                .Where(p => p.Key == key && p.EffectiveFrom <= date && (p.EffectiveTo == null || p.EffectiveTo >= date))
                .OrderByDescending(p => p.EffectiveFrom)
                .FirstOrDefaultAsync();

            if (param == null)
                throw new BusinessException($"Parámetro '{key}' no encontrado para la fecha {date:yyyy-MM-dd}.");

            if (!TryParseDecimalFlexible(param.Value, out var value))
                throw new BusinessException($"Valor inválido para parámetro '{key}': '{param.Value}'.");

            if (key.Equals("UVT", StringComparison.OrdinalIgnoreCase) && value <= 0m)
                throw new BusinessException("UVT debe ser mayor que 0.");

            return value;
        }

        private static bool TryParseDecimalFlexible(string raw, out decimal value)
        {
            raw = raw?.Trim() ?? string.Empty;
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) return true;
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.GetCultureInfo("es-CO"), out value)) return true;
            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }
    }
}
