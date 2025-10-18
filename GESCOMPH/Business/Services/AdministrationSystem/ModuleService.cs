using Business.Interfaces.Implements.AdministrationSystem;
using Business.Repository;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.DTOs.Implements.AdministrationSystem.Module;
using MapsterMapper;
using System.Linq.Expressions;

namespace Business.Services.AdministrationSystem
{
    public class ModuleService 
        : BusinessGeneric<ModuleSelectDto, ModuleCreateDto, ModuleUpdateDto, Module>, 
          IModuleService
    {
        public ModuleService(IDataGeneric<Module> data, IMapper mapper) 
            : base(data, mapper)
        {
        }

        /// <summary>
        /// Aplica una regla de unicidad para evitar módulos duplicados basados en el nombre.
        /// </summary>
        /// <param name="query">Consulta base sobre los módulos existentes.</param>
        /// <param name="candidate">Entidad candidata a insertar o actualizar.</param>
        /// <returns>Consulta filtrada que detecta duplicados por nombre.</returns>
        protected override IQueryable<Module>? ApplyUniquenessFilter(IQueryable<Module> query, Module candidate)
            => query.Where(m => m.Name == candidate.Name);

        /// <summary>
        /// Define los campos de texto que pueden ser utilizados en búsquedas.
        /// </summary>
        /// <returns>Expresiones que representan los campos buscables.</returns>
        protected override Expression<Func<Module, string>>[] SearchableFields() =>
        [
            m => m.Name!,
            m => m.Description!,
            m => m.Icon!
        ];

        /// <summary>
        /// Define los campos que pueden ser utilizados para ordenar los resultados.
        /// </summary>
        /// <returns>Arreglo con los nombres de los campos ordenables.</returns>
        protected override string[] SortableFields() =>
        [
            nameof(Module.Name),
            nameof(Module.Description),
            nameof(Module.Icon),
            nameof(Module.Active),
            nameof(Module.CreatedAt),
            nameof(Module.Id)
        ];
    }
}
