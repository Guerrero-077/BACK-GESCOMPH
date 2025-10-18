using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Repository;
using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.RolUser;
using MapsterMapper;
using System.Linq.Expressions;

namespace Business.Services.SecurityAuthentication
{
    /// <summary>
    /// Servicio encargado de gestionar las relaciones entre usuarios y roles.
    /// Permite consultar, asignar y reemplazar roles asociados a un usuario,
    /// manteniendo coherencia con las reglas de seguridad del sistema.
    /// </summary>
    public class RolUserService
        : BusinessGeneric<RolUserSelectDto, RolUserCreateDto, RolUserUpdateDto, RolUser>,
          IRolUserService
    {
        private readonly IRolUserRepository _repository;

        /// <summary>
        /// Inicializa una nueva instancia del servicio de roles por usuario.
        /// </summary>
        /// <param name="data">Repositorio de acceso a datos para <see cref="RolUser"/>.</param>
        /// <param name="mapper">Instancia del mapeador para entidades y DTOs.</param>
        public RolUserService(IRolUserRepository data, IMapper mapper)
            : base(data, mapper)
        {
            _repository = data;
        }

        /// <summary>
        /// Obtiene los nombres de roles asociados a un usuario específico.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <returns>Lista de nombres de roles asignados al usuario.</returns>
        public async Task<IEnumerable<string>> GetRoleNamesByUserIdAsync(int userId)
        {
            return await _repository.GetRoleNamesByUserIdAsync(userId);
        }

        /// <summary>
        /// Reemplaza los roles actuales de un usuario con un nuevo conjunto de roles.
        /// La operación es idempotente: se eliminan roles no incluidos y se agregan los nuevos.
        /// </summary>
        /// <param name="userId">Identificador del usuario.</param>
        /// <param name="roleIds">Lista de identificadores de roles a asignar.</param>
        public Task ReplaceUserRolesAsync(int userId, IEnumerable<int> roleIds)
            => _repository.ReplaceUserRolesAsync(userId, roleIds);

        /// <summary>
        /// Define los campos de texto sobre los que se puede realizar búsqueda dinámica.
        /// </summary>
        /// <returns>Expresiones con los campos buscables.</returns>
        protected override Expression<Func<RolUser, string?>>[] SearchableFields() =>
        [
            x => x.Rol.Name,
            x => x.User.Email
        ];

        /// <summary>
        /// Define los campos que pueden utilizarse para ordenamiento dinámico
        /// en consultas de relaciones usuario-rol.
        /// </summary>
        /// <returns>Lista de nombres de campos ordenables.</returns>
        protected override string[] SortableFields() =>
        [
            nameof(RolUser.RolId),
            nameof(RolUser.UserId),
            nameof(RolUser.Id),
            nameof(RolUser.CreatedAt),
            nameof(RolUser.Active)
        ];

        /// <summary>
        /// Define los filtros personalizados que pueden aplicarse
        /// mediante parámetros de búsqueda sobre relaciones usuario-rol.
        /// </summary>
        /// <returns>Diccionario de filtros permitidos y sus expresiones.</returns>
        protected override IDictionary<string, Func<string, Expression<Func<RolUser, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<RolUser, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(RolUser.RolId)]   = value => x => x.RolId == int.Parse(value),
                [nameof(RolUser.UserId)]  = value => x => x.UserId == int.Parse(value),
                [nameof(RolUser.Active)]  = value => x => x.Active == bool.Parse(value)
            };
    }
}
