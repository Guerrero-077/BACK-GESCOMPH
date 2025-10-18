namespace Business.CustomJWT;

/// <summary>
/// Implementación de <see cref="IClock"/> basada en <see cref="DateTime.UtcNow"/>.
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

