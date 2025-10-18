namespace Business.Interfaces.Implements.SecurityAuthentication.Tokens;

/// <summary>
/// Abstracción para el cálculo de hash de tokens (p.ej., refresh tokens) con algoritmos seguros.
/// </summary>
public interface ITokenHasher
{
    /// <summary>
    /// Calcula el hash del valor dado.
    /// </summary>
    /// <param name="token">Valor en claro.</param>
    /// <returns>Hash en representación de texto (p.ej., hex).</returns>
    string Hash(string token);
}

