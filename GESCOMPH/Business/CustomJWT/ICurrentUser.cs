namespace Business.CustomJWT
{
    /// <summary>
    /// Define un contrato para obtener información del usuario actualmente autenticado 
    /// dentro del contexto de ejecución.
    /// 
    /// Esta interfaz abstrae el acceso a los datos del usuario (por ejemplo, 
    /// obtenidos desde un token JWT) y facilita la validación de roles o permisos 
    /// específicos dentro de la capa de negocio.
    /// 
    /// Es comúnmente utilizada en servicios o controladores que requieren conocer 
    /// la identidad y privilegios del usuario que realiza la operación.
    /// </summary>
    public interface ICurrentUser
    {
        /// <summary>
        /// Identificador de la persona asociada al usuario autenticado.
        /// </summary>
        /// <remarks>
        /// Puede ser <c>null</c> si no existe un usuario autenticado en el contexto actual.
        /// </remarks>
        int? PersonId { get; }

        /// <summary>
        /// Verifica si el usuario actual pertenece a un rol específico.
        /// </summary>
        /// <param name="role">Nombre del rol a verificar.</param>
        /// <returns>
        /// <c>true</c> si el usuario pertenece al rol especificado; 
        /// de lo contrario, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Este método permite evaluar permisos basados en roles definidos 
        /// dentro del token JWT o la identidad del usuario.
        /// </remarks>
        bool IsInRole(string role);

        /// <summary>
        /// Indica si el usuario autenticado tiene privilegios de administrador.
        /// </summary>
        /// <remarks>
        /// Esta propiedad actúa como un atajo para <see cref="IsInRole(string)"/> 
        /// cuando el rol es equivalente a "Administrador".
        /// </remarks>
        bool EsAdministrador { get; }

        /// <summary>
        /// Indica si el usuario autenticado tiene el rol de arrendador.
        /// </summary>
        /// <remarks>
        /// Similar a <see cref="EsAdministrador"/>, esta propiedad simplifica la 
        /// comprobación de permisos específicos sin requerir verificar el nombre 
        /// del rol manualmente.
        /// </remarks>
        bool EsArrendador { get; }
    }
}
