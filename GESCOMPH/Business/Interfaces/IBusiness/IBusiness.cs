using Entity.DTOs.Base;

namespace Business.Interfaces.IBusiness
{
    /// <summary>
    /// Define un contrato genérico para operaciones de negocio (Business Layer)
    /// que gestionan entidades a través de Data Transfer Objects (DTOs).
    /// 
    /// Esta interfaz provee métodos asíncronos para operaciones CRUD básicas,
    /// eliminación lógica, actualización de estado activo e implementación
    /// de consultas paginadas.
    /// 
    /// Su objetivo es estandarizar el comportamiento de los servicios de negocio
    /// y facilitar su uso con distintos tipos de entidades.
    /// </summary>
    /// <typeparam name="TDtoGet">
    /// Tipo del DTO usado para las operaciones de lectura (consultas).
    /// </typeparam>
    /// <typeparam name="TDtoCreate">
    /// Tipo del DTO usado para las operaciones de creación.
    /// </typeparam>
    /// <typeparam name="TDtoUpdate">
    /// Tipo del DTO usado para las operaciones de actualización.
    /// </typeparam>
    public interface IBusiness<TDtoGet, TDtoCreate, TDtoUpdate>
    {
        /// <summary>
        /// Obtiene todos los registros disponibles.
        /// </summary>
        /// <returns>
        /// Una colección enumerable de <typeparamref name="TDtoGet"/>.
        /// </returns>
        Task<IEnumerable<TDtoGet>> GetAllAsync();

        /// <summary>
        /// Obtiene un registro específico por su identificador único.
        /// </summary>
        /// <param name="id">Identificador del registro a consultar.</param>
        /// <returns>
        /// El DTO correspondiente si existe; de lo contrario, <c>null</c>.
        /// </returns>
        Task<TDtoGet?> GetByIdAsync(int id);

        /// <summary>
        /// Crea un nuevo registro a partir del DTO especificado.
        /// </summary>
        /// <param name="dto">DTO con los datos para crear el nuevo registro.</param>
        /// <returns>
        /// El DTO resultante después de la creación.
        /// </returns>
        Task<TDtoGet> CreateAsync(TDtoCreate dto);

        /// <summary>
        /// Actualiza un registro existente con los datos proporcionados.
        /// </summary>
        /// <param name="dto">DTO con los datos actualizados.</param>
        /// <returns>
        /// El DTO actualizado correspondiente al registro modificado.
        /// </returns>
        Task<TDtoGet> UpdateAsync(TDtoUpdate dto);

        /// <summary>
        /// Elimina físicamente un registro de la base de datos.
        /// </summary>
        /// <param name="id">Identificador del registro a eliminar.</param>
        /// <returns>
        /// <c>true</c> si la eliminación fue exitosa; de lo contrario, <c>false</c>.
        /// </returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Realiza una eliminación lógica (soft delete) de un registro.
        /// </summary>
        /// <param name="id">Identificador del registro a eliminar lógicamente.</param>
        /// <returns>
        /// <c>true</c> si la operación fue exitosa; de lo contrario, <c>false</c>.
        /// </returns>
        Task<bool> DeleteLogicAsync(int id);

        /// <summary>
        /// Actualiza el estado activo de un registro sin eliminarlo.
        /// </summary>
        /// <param name="id">Identificador del registro.</param>
        /// <param name="active">Valor booleano que indica el nuevo estado.</param>
        Task UpdateActiveStatusAsync(int id, bool active);

        /// <summary>
        /// Ejecuta una consulta paginada y filtrada según los criterios especificados.
        /// </summary>
        /// <param name="query">
        /// Objeto <see cref="PageQuery"/> que contiene los parámetros de paginación y filtro.
        /// </param>
        /// <returns>
        /// Un resultado paginado con una colección de <typeparamref name="TDtoGet"/>.
        /// </returns>
        Task<PagedResult<TDtoGet>> QueryAsync(PageQuery query);
    }
}
