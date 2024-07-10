using System.Text.Json.Serialization;
using MediatR;

namespace Dex.Audit.MediatR.Requests;

/// <summary>
/// Интерфейс для запросов аудита, которые возвращают результат типа <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">Тип результата запроса.</typeparam>
public abstract class AuditRequest<TResponse> : IRequest<TResponse>
{
    /// <summary>
    /// Тип события аудита.
    /// </summary>
    [JsonIgnore]
    public abstract string EventType { get; }

    /// <summary>
    /// Объект события аудита.
    /// </summary>
    [JsonIgnore]
    public abstract string EventObject { get; }

    /// <summary>
    /// Сообщение события аудита.
    /// </summary>
    [JsonIgnore]
    public abstract string Message { get; }
}
