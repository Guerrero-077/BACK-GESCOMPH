using Business.Interfaces.Implements.AdministrationSystem;
using Business.Repository;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.DTOs.Implements.AdministrationSystem.SystemParameter;
using MapsterMapper;
using System.Linq.Expressions;
using Utilities.Exceptions;

namespace Business.Services.AdministrationSystem
{
    public class SystemParameterService 
        : BusinessGeneric<SystemParameterSelectDto, SystemParameterDto, SystemParameterUpdateDto, SystemParameter>, 
          ISystemParameterService
    {
        public SystemParameterService(IDataGeneric<SystemParameter> data, IMapper mapper) 
            : base(data, mapper)
        {
        }

        /// <summary>
        /// Define los campos de la entidad que son buscables mediante operaciones de texto.
        /// </summary>
        /// <returns>Expresiones que representan los campos buscables.</returns>
        protected override Expression<Func<SystemParameter, string>>[] SearchableFields() =>
        [
            sp => sp.Key!,
            sp => sp.Value!
        ];

        /// <summary>
        /// Define los campos que se pueden utilizar para ordenar los resultados de las consultas.
        /// </summary>
        /// <returns>Arreglo de nombres de campos ordenables.</returns>
        protected override string[] SortableFields() => new[]
        {
            nameof(SystemParameter.Key),
            nameof(SystemParameter.Value),
            nameof(SystemParameter.EffectiveFrom),
            nameof(SystemParameter.EffectiveTo),
            nameof(SystemParameter.Active),
            nameof(SystemParameter.Id),
            nameof(SystemParameter.CreatedAt)
        };

        /// <summary>
        /// Define los filtros permitidos para la búsqueda y consulta de parámetros del sistema.
        /// </summary>
        /// <returns>Diccionario de filtros válidos y sus expresiones asociadas.</returns>
        protected override IDictionary<string, Func<string, Expression<Func<SystemParameter, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<SystemParameter, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(SystemParameter.Key)] = v => e => e.Key == v,
                [nameof(SystemParameter.Active)] = v => e => e.Active == bool.Parse(v)
            };

        /// <summary>
        /// Aplica una regla de unicidad para evitar duplicados basados en la clave (<see cref="SystemParameter.Key"/>).
        /// </summary>
        /// <param name="query">Consulta base.</param>
        /// <param name="candidate">Entidad candidata.</param>
        /// <returns>Consulta filtrada para detectar duplicados.</returns>
        protected override IQueryable<SystemParameter>? ApplyUniquenessFilter(
            IQueryable<SystemParameter> query, 
            SystemParameter candidate) 
            => query.Where(sp => sp.Key == candidate.Key);

        /// <summary>
        /// Valida las fechas de vigencia, asegurando que la fecha de finalización no sea anterior a la de inicio.
        /// </summary>
        /// <param name="dto">DTO del parámetro del sistema.</param>
        /// <exception cref="BusinessException">Si las fechas son inválidas.</exception>
        private static void ValidateDates(ISystemParameterDto dto)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            var from = dto.EffectiveFrom;
            var to = dto.EffectiveTo;

            if (to.HasValue && to.Value.Date < from.Date)
                throw new BusinessException("La fecha 'Vigente hasta' no puede ser menor que 'Vigente desde'.");
        }

        /// <summary>
        /// Crea un nuevo parámetro del sistema, validando y normalizando la información.
        /// </summary>
        /// <param name="dto">Datos del parámetro a crear.</param>
        /// <returns>El parámetro creado.</returns>
        public override async Task<SystemParameterSelectDto> CreateAsync(SystemParameterDto dto)
        {
            ValidateDates(dto);

            dto.Key = dto.Key?.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Key))
                dto.Key = dto.Key.ToUpperInvariant();

            dto.Value = dto.Value?.Trim();

            return await base.CreateAsync(dto);
        }

        /// <summary>
        /// Actualiza un parámetro del sistema existente, validando y normalizando la información.
        /// </summary>
        /// <param name="dto">Datos del parámetro a actualizar.</param>
        /// <returns>El parámetro actualizado, o null si no se encuentra.</returns>
        public override async Task<SystemParameterSelectDto?> UpdateAsync(SystemParameterUpdateDto dto)
        {
            ValidateDates(dto);

            dto.Key = dto.Key?.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Key))
                dto.Key = dto.Key.ToUpperInvariant();

            dto.Value = dto.Value?.Trim();

            return await base.UpdateAsync(dto);
        }
    }
}
