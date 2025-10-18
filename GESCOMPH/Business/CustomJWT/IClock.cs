namespace Business.CustomJWT;

/// <summary>
/// Fuente de tiempo intercambiable para operaciones que dependen de DateTime.UtcNow.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Hora actual en UTC.
    /// </summary>
    DateTime UtcNow { get; }
}

