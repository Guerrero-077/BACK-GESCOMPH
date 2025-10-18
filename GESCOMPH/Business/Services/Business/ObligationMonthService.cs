using Business.Interfaces.Implements.Business;
using Business.Repository;
using Data.Interfaz.DataBasic;
using Data.Interfaz.IDataImplement.Business;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.ObligationMonth;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Linq.Expressions;
using Utilities.Exceptions;

namespace Business.Services.Business
{
    /// <summary>
    /// Servicio de negocio encargado de gestionar las obligaciones mensuales
    /// asociadas a contratos, incluyendo generación automática, cálculo de montos
    /// y validación de parámetros financieros (UVT, IVA).
    /// </summary>
    public class ObligationMonthService
        : BusinessGeneric<ObligationMonthSelectDto, ObligationMonthDto, ObligationMonthUpdateDto, ObligationMonth>,
          IObligationMonthService
    {
        private readonly IObligationMonthRepository _obligationRepository;
        private readonly IContractRepository _contractRepository;
        private readonly IDataGeneric<SystemParameter> _systemParamRepository;

        /// <summary>
        /// Inicializa una nueva instancia del servicio de obligaciones mensuales.
        /// </summary>
        public ObligationMonthService(
            IObligationMonthRepository obligationRepository,
            IContractRepository contractRepository,
            IDataGeneric<SystemParameter> systemParamRepository,
            IMapper mapper)
            : base(obligationRepository, mapper)
        {
            _obligationRepository = obligationRepository;
            _contractRepository = contractRepository;
            _systemParamRepository = systemParamRepository;
        }

        /// <summary>
        /// Genera las obligaciones mensuales para todos los contratos activos
        /// correspondientes al año y mes indicados.
        /// </summary>
        public async Task GenerateMonthlyAsync(int year, int month)
        {
            var (monthStart, monthEnd, dueDate) = GetPeriodDates(year, month);
            var uvtValue = await GetParameterValueAsync("UVT", dueDate);
            var vatRate = await GetParameterValueAsync("IVA", dueDate);

            var contracts = await _contractRepository.GetAllQueryable()
                .Where(c => c.Active && c.StartDate < monthEnd && c.EndDate >= monthStart)
                .ToListAsync();

            foreach (var contract in contracts)
                await UpsertObligationAsync(contract, monthStart, uvtValue, vatRate);
        }

        /// <summary>
        /// Genera la obligación mensual para un contrato específico, validando que
        /// el contrato esté vigente durante el período.
        /// </summary>
        public async Task GenerateForContractMonthAsync(int contractId, int year, int month)
        {
            var contract = await _contractRepository.GetByIdAsync(contractId)
                ?? throw new BusinessException("Contrato no existe.");

            var (monthStart, monthEnd, dueDate) = GetPeriodDates(year, month);

            if (!(contract.StartDate < monthEnd && contract.EndDate >= monthStart))
                return;

            var uvtValue = await GetParameterValueAsync("UVT", dueDate);
            var vatRate = await GetParameterValueAsync("IVA", dueDate);

            await UpsertObligationAsync(contract, monthStart, uvtValue, vatRate);
        }

        /// <summary>
        /// Retorna todas las obligaciones mensuales asociadas a un contrato.
        /// </summary>
        public async Task<IReadOnlyList<ObligationMonthSelectDto>> GetByContractAsync(int contractId)
        {
            if (contractId <= 0)
                throw new BusinessException("contractId inválido.");

            var list = await _obligationRepository.GetByContractQueryable(contractId)
                .AsNoTracking()
                .ToListAsync();

            return _mapper.Map<List<ObligationMonthSelectDto>>(list).AsReadOnly();
        }

        /// <summary>
        /// Marca una obligación mensual como pagada, asignando la fecha actual y bloqueando futuras modificaciones.
        /// </summary>
        public async Task MarkAsPaidAsync(int id)
        {
            var existing = await _obligationRepository.GetByIdAsync(id)
                ?? throw new BusinessException($"No existe obligación mensual con Id {id}.");

            existing.PaymentDate = DateTime.UtcNow;
            existing.Status = "PAID";
            existing.Locked = true;

            await _obligationRepository.UpdateAsync(existing);
        }

        // ------------------ Métodos internos de negocio ------------------

        /// <summary>
        /// Crea o actualiza (Upsert) una obligación mensual para un contrato determinado.
        /// Si ya existe y está bloqueada, se omite.
        /// </summary>
        private async Task UpsertObligationAsync(Contract contract, DateTime periodDate, decimal uvtValue, decimal vatRate)
        {
            var existing = await _obligationRepository
                .GetByContractYearMonthAsync(contract.Id, periodDate.Year, periodDate.Month);

            if (existing != null && existing.Locked)
                return;

            var (baseAmount, vatAmount, totalAmount) = CalculateAmounts(contract, uvtValue, vatRate);
            var dueDate = new DateTime(periodDate.Year, periodDate.Month, DateTime.DaysInMonth(periodDate.Year, periodDate.Month));

            if (existing == null)
            {
                var obligation = new ObligationMonth
                {
                    ContractId = contract.Id,
                    Year = periodDate.Year,
                    Month = periodDate.Month,
                    DueDate = dueDate,
                    UvtQtyApplied = contract.TotalUvtQtyAgreed,
                    UvtValueApplied = uvtValue,
                    VatRateApplied = vatRate,
                    BaseAmount = baseAmount,
                    VatAmount = vatAmount,
                    TotalAmount = totalAmount,
                    Status = "PENDING"
                };

                await _obligationRepository.AddAsync(obligation);
            }
            else
            {
                existing.UvtQtyApplied = contract.TotalUvtQtyAgreed;
                existing.UvtValueApplied = uvtValue;
                existing.VatRateApplied = vatRate;
                existing.BaseAmount = baseAmount;
                existing.VatAmount = vatAmount;
                existing.TotalAmount = totalAmount;

                if (existing.Status == "CANCELLED")
                    existing.Status = "PENDING";

                await _obligationRepository.UpdateAsync(existing);
            }
        }

        /// <summary>
        /// Calcula las fechas de inicio, fin y vencimiento de un período mensual.
        /// </summary>
        private (DateTime MonthStart, DateTime MonthEnd, DateTime DueDate) GetPeriodDates(int year, int month)
        {
            var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);
            var dueDate = new DateTime(year, month, DateTime.DaysInMonth(year, month), 0, 0, 0, DateTimeKind.Utc);
            return (monthStart, monthEnd, dueDate);
        }

        /// <summary>
        /// Calcula los montos base, IVA y total de una obligación mensual.
        /// </summary>
        private (decimal BaseAmount, decimal VatAmount, decimal TotalAmount) CalculateAmounts(Contract contract, decimal uvtValue, decimal vatRate)
        {
            decimal baseAmount = contract.TotalBaseRentAgreed > 0m
                ? contract.TotalBaseRentAgreed
                : contract.TotalUvtQtyAgreed * uvtValue;

            decimal vatAmount = baseAmount * vatRate;
            return (baseAmount, vatAmount, baseAmount + vatAmount);
        }

        /// <summary>
        /// Obtiene el valor vigente de un parámetro del sistema (UVT, IVA, etc.)
        /// según una fecha específica.
        /// </summary>
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

            if (key.Equals("IVA", StringComparison.OrdinalIgnoreCase))
            {
                if (value >= 1m) value /= 100m;
                if (value < 0m || value > 1m)
                    throw new BusinessException($"El parámetro 'IVA' debe estar entre 0 y 1. Recibido: {value}.");
            }

            if (key.Equals("UVT", StringComparison.OrdinalIgnoreCase) && value <= 0m)
                throw new BusinessException("UVT debe ser mayor que 0.");

            return value;
        }

        /// <summary>
        /// Intenta convertir un valor string a decimal considerando formato local, invariante y actual.
        /// </summary>
        private static bool TryParseDecimalFlexible(string raw, out decimal value)
        {
            raw = raw?.Trim() ?? "";
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) return true;
            var es = CultureInfo.GetCultureInfo("es-CO");
            if (decimal.TryParse(raw, NumberStyles.Any, es, out value)) return true;
            return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }

        /// <summary>
        /// Retorna el total de obligaciones pagadas en una fecha específica.
        /// </summary>
        public async Task<decimal> GetTotalObligationsPaidByDayAsync(DateTime date)
        {
            return await _obligationRepository.GetTotalObligationsPaidByDayAsync(date);
        }

        /// <summary>
        /// Retorna el total de obligaciones pagadas en un mes y año específicos.
        /// </summary>
        public async Task<decimal> GetTotalObligationsPaidByMonthAsync(int year, int month)
        {
            return await _obligationRepository.GetTotalObligationsPaidByMonthAsync(year, month);
        }

        /// <summary>
        /// Campos habilitados para búsqueda textual.
        /// </summary>
        protected override Expression<Func<ObligationMonth, string>>[] SearchableFields() =>
        [
            e => e.Status
        ];

        /// <summary>
        /// Campos habilitados para ordenamiento dinámico.
        /// </summary>
        protected override string[] SortableFields() =>
        [
            nameof(ObligationMonth.Year),
            nameof(ObligationMonth.Month),
            nameof(ObligationMonth.DueDate),
            nameof(ObligationMonth.BaseAmount),
            nameof(ObligationMonth.TotalAmount),
            nameof(ObligationMonth.LateAmount),
            nameof(ObligationMonth.Status),
            nameof(ObligationMonth.Locked),
            nameof(ObligationMonth.Active),
            nameof(ObligationMonth.CreatedAt),
            nameof(ObligationMonth.Id)
        ];

        /// <summary>
        /// Filtros permitidos para consultas mediante query params.
        /// </summary>
        protected override IDictionary<string, Func<string, Expression<Func<ObligationMonth, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<ObligationMonth, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(ObligationMonth.ContractId)] = val => e => e.ContractId == int.Parse(val),
                [nameof(ObligationMonth.Year)] = val => e => e.Year == int.Parse(val),
                [nameof(ObligationMonth.Month)] = val => e => e.Month == int.Parse(val),
                [nameof(ObligationMonth.Status)] = val => e => e.Status == val,
                [nameof(ObligationMonth.Locked)] = val => e => e.Locked == bool.Parse(val),
                [nameof(ObligationMonth.Active)] = val => e => e.Active == bool.Parse(val),
                [nameof(ObligationMonth.DueDate)] = val => e => e.DueDate.Date == DateTime.Parse(val).Date
            };
    }
}
