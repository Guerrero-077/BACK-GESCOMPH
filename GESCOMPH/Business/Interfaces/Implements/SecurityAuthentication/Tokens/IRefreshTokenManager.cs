namespace Business.Interfaces.Implements.SecurityAuthentication.Tokens;

using Entity.DTOs.Implements.SecurityAuthentication.Auth;

/// <summary>
/// Contrato para gestionar el ciclo de vida de Refresh Tokens:
/// emisión, rotación segura y revocación.
/// </summary>
public interface IRefreshTokenManager
{
    /// <summary>
    /// Emite un nuevo refresh token para el usuario.
    /// </summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <returns>Resultado con el valor en claro, hash y expiración.</returns>
    Task<RefreshIssueResult> IssueAsync(int userId);

    /// <summary>
    /// Rota un refresh token válido, detectando reutilización y generando uno nuevo.
    /// </summary>
    /// <param name="currentPlain">Valor en claro del refresh token actual.</param>
    /// <returns>Resultado con el usuario y el nuevo token emitido.</returns>
    Task<RefreshRotateResult> RotateAsync(string currentPlain);

    /// <summary>
    /// Revoca explícitamente un refresh token por su valor en claro.
    /// </summary>
    Task RevokeAsync(string plainToken);

    /// <summary>
    /// Revoca todos los refresh tokens activos de un usuario.
    /// </summary>
    Task RevokeAllAsync(int userId);
}

/// <summary>
/// Resultado de emisión de refresh token.
/// </summary>
public readonly record struct RefreshIssueResult(string Plain, string Hash, DateTime ExpiresAt);

/// <summary>
/// Resultado de rotación de refresh token con datos del usuario.
/// </summary>
public readonly record struct RefreshRotateResult(UserAuthDto User, string NewPlain, string NewHash, DateTime ExpiresAt);

