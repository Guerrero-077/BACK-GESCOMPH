namespace Business.Interfaces.Implements.SecurityAuthentication.Tokens;

/// <summary>
/// Generador de cadenas aleatorias seguras (formato URL-safe recomendado para headers/cookies).
/// </summary>
public interface IRandomTokenGenerator
{
    /// <summary>
    /// Genera un token con la entropía indicada en bytes.
    /// </summary>
    /// <param name="bytes">Cantidad de bytes de entropía.</param>
    /// <returns>Cadena aleatoria URL-safe.</returns>
    string Generate(int bytes);
}

