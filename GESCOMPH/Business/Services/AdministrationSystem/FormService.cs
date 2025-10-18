using Business.Interfaces.Implements.AdministrationSystem;
using Business.Repository;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.DTOs.Implements.AdministrationSystem.Form;
using MapsterMapper;
using System.Linq.Expressions;

namespace Business.Services.AdministrationSystem
{
    /// <summary>
    /// Servicio de negocio para la gestión de formularios del sistema.
    /// Implementa operaciones CRUD y define reglas específicas de búsqueda, filtrado y unicidad.
    /// </summary>
    public class FormService 
        : BusinessGeneric<FormSelectDto, FormCreateDto, FormUpdateDto, Form>, 
          IFormService
    {
        public FormService(IDataGeneric<Form> data, IMapper mapper) 
            : base(data, mapper)
        {
        }

        /// <summary>
        /// Define la regla de unicidad para evitar formularios duplicados basados en el nombre.
        /// </summary>
        /// <param name="query">Consulta base de formularios existentes.</param>
        /// <param name="candidate">Entidad candidata a crear o actualizar.</param>
        /// <returns>Consulta filtrada para detectar duplicados.</returns>
        protected override IQueryable<Form>? ApplyUniquenessFilter(IQueryable<Form> query, Form candidate)
            => query.Where(f => f.Name == candidate.Name);

        /// <summary>
        /// Campos habilitados para búsquedas de texto parcial o exacta.
        /// </summary>
        /// <returns>Arreglo de expresiones con los campos buscables.</returns>
        protected override Expression<Func<Form, string>>[] SearchableFields() =>
        [
            f => f.Name!,
            f => f.Description!,
            f => f.Route!
        ];

        /// <summary>
        /// Define los filtros explícitamente permitidos por query parameters.
        /// </summary>
        /// <returns>Diccionario con filtros por propiedad y su expresión asociada.</returns>
        protected override IDictionary<string, Func<string, Expression<Func<Form, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<Form, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Form.Route)] = value => entity => entity.Route == value,
                [nameof(Form.Active)] = value => entity => entity.Active == bool.Parse(value)
            };
    }
}
