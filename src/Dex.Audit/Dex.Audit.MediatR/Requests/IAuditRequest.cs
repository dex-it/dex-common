using MediatR;

namespace Dex.Audit.MediatR.Requests;

/// <summary>
/// Интерфейс для запросов аудита, которые возвращают результат типа <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">Тип результата запроса.</typeparam>
public interface IAuditRequest<out TResponse> : IRequest<TResponse>
{
    /// <summary>
    /// Тип события аудита.
    /// </summary>
    public string EventType { get; }

    /// <summary>
    /// Объект события аудита.
    /// </summary>
    public string EventObject { get; }

    /// <summary>
    /// Сообщение события аудита.
    /// </summary>
    public string Message { get; }
}
