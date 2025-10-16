using Business.Interfaces.IBusiness;
using Entity.Domain.Models.ModelBase;
using Entity.DTOs.Base;

namespace Business.Repository
{
    /// <summary>
    /// Clase base abstracta que define operaciones genéricas de negocio (CRUD y consulta paginada)
    /// sobre entidades del dominio y sus correspondientes DTOs.
    /// 
    /// Este patrón centraliza la lógica de acceso de alto nivel para servicios de aplicación,
    /// proporcionando una interfaz consistente y reutilizable para distintos tipos de entidades.
    /// </summary>
    /// <typeparam name="TDtoGet">Tipo del DTO usado para devolver información al cliente.</typeparam>
    /// <typeparam name="TDtoCreate">Tipo del DTO usado para crear nuevas entidades.</typeparam>
    /// <typeparam name="TDtoUpdate">Tipo del DTO usado para actualizar entidades existentes.</typeparam>
    /// <typeparam name="TEntity">Tipo de entidad del dominio asociada, que debe heredar de <see cref="BaseModel"/>.</typeparam>
    public abstract class ABusinessGeneric<TDtoGet, TDtoCreate, TDtoUpdate, TEntity>
        : IBusiness<TDtoGet, TDtoCreate, TDtoUpdate> where TEntity : BaseModel
    {
        /// <summary>
        /// Obtiene todas las entidades y las proyecta al tipo <typeparamref name="TDtoGet"/>.
        /// </summary>
        /// <returns>Colección de objetos DTO representando todas las entidades existentes.</returns>
        public abstract Task<IEnumerable<TDtoGet>> GetAllAsync();

        /// <summary>
        /// Recupera una entidad por su identificador único.
        /// </summary>
        /// <param name="id">Identificador numérico de la entidad.</param>
        /// <returns>Un DTO de tipo <typeparamref name="TDtoGet"/> o <c>null</c> si no existe.</returns>
        public abstract Task<TDtoGet?> GetByIdAsync(int id);

        /// <summary>
        /// Crea una nueva entidad en base a los datos del DTO de creación.
        /// </summary>
        /// <param name="dto">Datos de creación de la entidad.</param>
        /// <returns>DTO del registro creado.</returns>
        public abstract Task<TDtoGet> CreateAsync(TDtoCreate dto);

        /// <summary>
        /// Actualiza una entidad existente utilizando el DTO de actualización.
        /// </summary>
        /// <param name="dto">Datos de actualización de la entidad.</param>
        /// <returns>DTO actualizado de la entidad.</returns>
        public abstract Task<TDtoGet> UpdateAsync(TDtoUpdate dto);

        /// <summary>
        /// Elimina físicamente una entidad del sistema (borrado permanente).
        /// </summary>
        /// <param name="id">Identificador de la entidad a eliminar.</param>
        /// <returns><c>true</c> si la operación fue exitosa; de lo contrario, <c>false</c>.</returns>
        public abstract Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Realiza un borrado lógico de la entidad, marcándola como inactiva o eliminada sin eliminarla de la base de datos.
        /// </summary>
        /// <param name="id">Identificador de la entidad a eliminar lógicamente.</param>
        /// <returns><c>true</c> si la operación fue exitosa; de lo contrario, <c>false</c>.</returns>
        public abstract Task<bool> DeleteLogicAsync(int id);

        /// <summary>
        /// Cambia el estado de activación de una entidad (activar o desactivar).
        /// </summary>
        /// <param name="id">Identificador de la entidad.</param>
        /// <param name="active">Valor booleano que indica si la entidad debe estar activa (<c>true</c>) o inactiva (<c>false</c>).</param>
        public abstract Task UpdateActiveStatusAsync(int id, bool active);

        /// <summary>
        /// Ejecuta una consulta genérica sobre las entidades con soporte para paginación, búsqueda y filtros.
        /// Devuelve una colección paginada de DTOs de lectura.
        /// </summary>
        /// <param name="query">Parámetros de búsqueda, ordenamiento y paginación.</param>
        /// <returns>Resultado paginado con los elementos encontrados.</returns>
        /// <remarks>
        /// Este método se usa típicamente para endpoints de listado dinámico en APIs (e.g. tablas filtrables o paginadas).
        /// </remarks>
        public abstract Task<PagedResult<TDtoGet>> QueryAsync(PageQuery query);
    }
}
