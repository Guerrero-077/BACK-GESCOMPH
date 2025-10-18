namespace Business.Interfaces.Implements.SecurityAuthentication.Tokens;

using Entity.DTOs.Implements.SecurityAuthentication.Auth;

/// <summary>
/// Fabrica responsable de construir Access Tokens (JWT) para un usuario autenticado.
/// </summary>
public interface IAccessTokenFactory
{
    /// <summary>
    /// Crea un JWT firmado con los claims necesarios para el usuario dado.
    /// </summary>
    /// <param name="user">Datos mínimos del usuario para la emisión del token.</param>
    /// <returns>Cadena JWT firmada.</returns>
    string Create(UserAuthDto user);
}

