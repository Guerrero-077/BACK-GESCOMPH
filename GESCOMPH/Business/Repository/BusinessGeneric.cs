using Data.Interfaz.DataBasic;
using Entity.Domain.Models.ModelBase;
using Entity.DTOs.Base;
using Entity.DTOs.Implements.Business.Plaza;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Utilities.Exceptions;
using Utilities.Helpers.Business;

namespace Business.Repository
{
    /// <summary>
    /// Implementación genérica de la capa de negocio que provee operaciones CRUD,
    /// eliminación lógica, filtrado, búsqueda y ordenamiento.
    /// 
    /// Este servicio genérico:
    /// - Mapea entidades ↔ DTOs mediante <see cref="IMapper"/> (Mapster).
    /// - Soporta reactivación de entidades eliminadas lógicamente.
    /// - Permite validación de unicidad mediante <see cref="ApplyUniquenessFilter"/>.
    /// - Implementa consultas genéricas con paginación, búsqueda y filtros controlados.
    /// </summary>
    /// <typeparam name="TDtoGet">Tipo del DTO utilizado para lectura.</typeparam>
    /// <typeparam name="TDtoCreate">Tipo del DTO utilizado para creación.</typeparam>
    /// <typeparam name="TDtoUpdate">Tipo del DTO utilizado para actualización.</typeparam>
    /// <typeparam name="TEntity">Tipo de entidad del dominio, derivada de <see cref="BaseModel"/>.</typeparam>
    public class BusinessGeneric<TDtoGet, TDtoCreate, TDtoUpdate, TEntity>
        : ABusinessGeneric<TDtoGet, TDtoCreate, TDtoUpdate, TEntity> where TEntity : BaseModel
    {
        /// <summary>
        /// Repositorio genérico de datos asociado al tipo de entidad.
        /// </summary>
        protected readonly IDataGeneric<TEntity> Data;

        /// <summary>
        /// Mapper utilizado para transformar entidades ↔ DTOs (Mapster).
        /// </summary>
        protected readonly IMapper _mapper;

        /// <summary>
        /// Inicializa una nueva instancia del servicio de negocio genérico.
        /// </summary>
        /// <param name="data">Repositorio genérico de acceso a datos.</param>
        /// <param name="mapper">Mapper configurado para las conversiones DTO ↔ Entidad.</param>
        public BusinessGeneric(IDataGeneric<TEntity> data, IMapper mapper)
        {
            Data = data;
            _mapper = mapper;
        }

        /// <summary>
        /// Obtiene todos los registros activos.
        /// La capa de datos excluye automáticamente las entidades con <c>IsDeleted = true</c>.
        /// </summary>
        /// <returns>Enumeración de DTOs representando las entidades activas.</returns>
        /// <exception cref="BusinessException">Si ocurre un error en la operación.</exception>
        public override async Task<IEnumerable<TDtoGet>> GetAllAsync()
        {
            try
            {
                var entities = await Data.GetAllAsync();
                return _mapper.Map<IEnumerable<TDtoGet>>(entities);
            }
            catch (Exception ex)
            {
                throw new BusinessException("Error al obtener todos los registros.", ex);
            }
        }

        /// <summary>
        /// Obtiene un registro activo por su identificador.
        /// </summary>
        /// <param name="id">Identificador único de la entidad.</param>
        /// <returns>DTO correspondiente o <c>null</c> si no se encuentra.</returns>
        /// <exception cref="BusinessException">Si ocurre un error o el ID no es válido.</exception>
        public override async Task<TDtoGet?> GetByIdAsync(int id)
        {
            try
            {
                BusinessValidationHelper.ThrowIfZeroOrLess(id, "El ID debe ser mayor que cero.");

                var entity = await Data.GetByIdAsync(id);
                return entity == null ? default : _mapper.Map<TDtoGet>(entity);
            }
            catch (Exception ex)
            {
                throw new BusinessException($"Error al obtener el registro con ID {id}.", ex);
            }
        }

        /// <summary>
        /// Aplica un filtro opcional de unicidad sobre la entidad candidata antes de crearla.
        /// Se puede sobreescribir en clases derivadas para definir criterios de duplicados.
        /// </summary>
        /// <param name="query">Consulta base de todas las entidades.</param>
        /// <param name="candidate">Entidad candidata a insertar.</param>
        /// <returns>Consulta filtrada o <c>null</c> si no se define una lógica de unicidad.</returns>
        protected virtual IQueryable<TEntity>? ApplyUniquenessFilter(IQueryable<TEntity> query, TEntity candidate)
            => null;

        /// <summary>
        /// Crea una nueva entidad a partir del DTO de creación.
        /// - Si ya existe un duplicado activo, lanza excepción.
        /// - Si existe un duplicado inactivo, lo reactiva.
        /// - Si no existe duplicado, crea un nuevo registro.
        /// </summary>
        /// <param name="dto">DTO con los datos para la creación.</param>
        /// <returns>DTO representando la entidad creada o reactivada.</returns>
        /// <exception cref="BusinessException">Si se detecta duplicado o falla la creación.</exception>
        public override async Task<TDtoGet> CreateAsync(TDtoCreate dto)
        {
            try
            {
                BusinessValidationHelper.ThrowIfNull(dto, "El DTO no puede ser nulo.");
                var candidate = _mapper.Map<TEntity>(dto);

                var query = ApplyUniquenessFilter(Data.GetAllQueryable(), candidate);
                if (query is not null)
                {
                    var existing = query.FirstOrDefault();
                    if (existing is not null)
                    {
                        if (!existing.IsDeleted)
                            throw new BusinessException("Ya existe un registro con los mismos datos.");

                        existing.IsDeleted = false;
                        _mapper.Map(dto, existing);
                        var updated = await Data.UpdateAsync(existing);
                        return _mapper.Map<TDtoGet>(updated);
                    }
                }

                candidate.InitializeLogicalState();
                var created = await Data.AddAsync(candidate);
                return _mapper.Map<TDtoGet>(created);
            }
            catch (DbUpdateException dbx)
            {
                throw new BusinessException("Violación de unicidad al crear el registro. Revisa valores únicos.", dbx);
            }
            catch (Exception ex)
            {
                throw new BusinessException("Error al crear el registro.", ex);
            }
        }

        /// <summary>
        /// Actualiza un registro existente basado en el DTO de actualización.
        /// </summary>
        /// <param name="dto">DTO con los datos actualizados.</param>
        /// <returns>DTO de la entidad actualizada.</returns>
        /// <exception cref="BusinessException">Si ocurre un error durante la actualización.</exception>
        public override async Task<TDtoGet> UpdateAsync(TDtoUpdate dto)
        {
            try
            {
                BusinessValidationHelper.ThrowIfNull(dto, "El DTO no puede ser nulo.");
                var entity = _mapper.Map<TEntity>(dto);
                var updated = await Data.UpdateAsync(entity);
                return _mapper.Map<TDtoGet>(updated);
            }
            catch (Exception ex)
            {
                throw new BusinessException("Error al actualizar el registro.", ex);
            }
        }

        /// <summary>
        /// Elimina físicamente un registro de la base de datos.
        /// </summary>
        /// <param name="id">Identificador de la entidad.</param>
        /// <returns><c>true</c> si la eliminación fue exitosa; de lo contrario, <c>false</c>.</returns>
        /// <exception cref="BusinessException">Si el registro está activo o la eliminación falla.</exception>
        public override async Task<bool> DeleteAsync(int id)
        {
            try
            {
                BusinessValidationHelper.ThrowIfZeroOrLess(id, "El ID debe ser mayor que cero.");

                var entity = await Data.GetByIdAsync(id);
                if (entity == null) return false;

                if (entity.Active)
                    throw new BusinessException("No se puede eliminar un registro que se encuentra activo.");

                return await Data.DeleteAsync(id);
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

        /// <summary>
        /// Realiza una eliminación lógica del registro (marcando <c>IsDeleted = true</c>).
        /// </summary>
        /// <param name="id">Identificador de la entidad.</param>
        /// <returns><c>true</c> si la operación fue exitosa; de lo contrario, <c>false</c>.</returns>
        /// <exception cref="BusinessException">Si ocurre un error durante la eliminación lógica.</exception>
        public override async Task<bool> DeleteLogicAsync(int id)
        {
            try
            {
                BusinessValidationHelper.ThrowIfZeroOrLess(id, "El ID debe ser mayor que cero.");

                var entity = await Data.GetByIdAsync(id);
                if (entity == null) return false;

                if (entity.Active)
                    throw new BusinessException("No se puede eliminar un registro que se encuentra activo.");

                return await Data.DeleteLogicAsync(id);
            }
            catch (Exception ex)
            {
                throw new BusinessException($"Error al eliminar lógicamente el registro con ID {id}.", ex);
            }
        }

        /// <summary>
        /// Cambia el estado de activación (<c>Active</c>) de una entidad.
        /// </summary>
        /// <param name="id">Identificador de la entidad.</param>
        /// <param name="active">Nuevo estado de activación.</param>
        /// <exception cref="BusinessException">Si la actualización falla o el registro no existe.</exception>
        public override async Task UpdateActiveStatusAsync(int id, bool active)
        {
            try
            {
                BusinessValidationHelper.ThrowIfZeroOrLess(id, "El ID debe ser mayor que cero.");
                var entity = await Data.GetByIdAsync(id)
                    ?? throw new KeyNotFoundException($"No se encontró el registro con ID {id}.");

                if (entity.Active == active) return;

                entity.Active = active;
                await Data.UpdateAsync(entity);
            }
            catch (Exception ex)
            {
                throw new BusinessException($"Error al actualizar el estado del registro con ID {id}.", ex);
            }
        }

        /// <summary>
        /// Define los campos de texto sobre los que se aplicará búsqueda con "Contains".
        /// </summary>
        /// <returns>Expresiones de campos buscables.</returns>
        /// <remarks>Puede sobrescribirse para definir columnas específicas de búsqueda.</remarks>
        protected virtual Expression<Func<TEntity, string>>[] SearchableFields()
            => Array.Empty<Expression<Func<TEntity, string>>>();

        /// <summary>
        /// Define los campos permitidos para ordenamiento.
        /// </summary>
        /// <returns>Lista blanca de nombres de propiedades ordenables.</returns>
        protected virtual string[] SortableFields()
            => Array.Empty<string>();

        /// <summary>
        /// Define un mapa de campos de ordenamiento personalizados.
        /// Si se proporciona, reemplaza el comportamiento por defecto basado en EF.Property.
        /// </summary>
        /// <returns>Diccionario de claves de orden → expresión de selección.</returns>
        protected virtual IDictionary<string, LambdaExpression> SortMap()
            => new Dictionary<string, LambdaExpression>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Define un conjunto de filtros permitidos para proteger las consultas dinámicas.
        /// </summary>
        /// <returns>Diccionario propiedad → constructor de expresión de filtro.</returns>
        protected virtual IDictionary<string, Func<string, Expression<Func<TEntity, bool>>>> AllowedFilters()
            => new Dictionary<string, Func<string, Expression<Func<TEntity, bool>>>>();

        /// <summary>
        /// Ejecuta una consulta genérica con soporte de búsqueda, filtros y ordenamiento seguros.
        /// </summary>
        /// <param name="query">Parámetros de consulta: búsqueda, paginación y orden.</param>
        /// <returns>Resultado paginado de DTOs.</returns>
        /// <exception cref="BusinessException">Si ocurre un error durante la consulta.</exception>
        /// <remarks>
        /// Este método valida los filtros y campos de ordenamiento permitidos antes de delegar
        /// la ejecución al repositorio de datos.
        /// </remarks>
        public override async Task<PagedResult<TDtoGet>> QueryAsync(PageQuery query)
        {
            try
            {
                var safeFilters = new List<Expression<Func<TEntity, bool>>>();
                if (query.Filters is not null)
                {
                    var allow = AllowedFilters();
                    foreach (var (k, v) in query.Filters)
                    {
                        if (allow.TryGetValue(k, out var builder))
                            safeFilters.Add(builder(v));
                    }
                }

                var sortOk = SortMap().ContainsKey(query.Sort ?? string.Empty)
                             || SortableFields().Contains(query.Sort, StringComparer.OrdinalIgnoreCase);

                if (!sortOk)
                    query = query with { Sort = null };

                var result = await Data.QueryAsync(
                    query,
                    SearchableFields(),
                    safeFilters.ToArray(),
                    SortMap()
                );

                return new PagedResult<TDtoGet>(
                    Items: _mapper.Map<IEnumerable<TDtoGet>>(result.Items).ToList(),
                    Total: result.Total,
                    Page: result.Page,
                    Size: result.Size
                );
            }
            catch (Exception ex)
            {
                throw new BusinessException("Error en consulta genérica.", ex);
            }
        }
    }
}
