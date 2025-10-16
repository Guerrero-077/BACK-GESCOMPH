namespace Entity.DTOs.Base
{
    /// <summary>
    /// Interfaz para exponer metadatos de paginación sin usar reflexión.
    /// </summary>
    public interface IPagedResult
    {
        int Total { get; }
        int Page { get; }
        int Size { get; }
        int TotalPages { get; }
    }
}

