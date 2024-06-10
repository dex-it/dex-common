namespace Dex.Audit.MediatR.Responses;

/// <summary>
/// Интерфейс для ответов аудита
/// </summary>
public interface IAuditResponse
{
    /// <summary>
    /// Успешность запроса
    /// </summary>
    public bool Success { get; }
}
