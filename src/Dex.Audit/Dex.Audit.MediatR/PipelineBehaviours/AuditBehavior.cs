using Dex.Audit.Client.Interfaces;
using Dex.Audit.Client.Messages;
using Dex.Audit.MediatR.Requests;
using Dex.Audit.MediatR.Responses;
using MediatR;

namespace Dex.Audit.MediatR.PipelineBehaviours;

/// <summary>
/// Представляет поведение аудита для использования в пайплайнах обработки запросов.
/// </summary>
/// <typeparam name="TRequest">Тип запроса, который должен реализовать интерфейс <see cref="IAuditRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">Тип ответа, который должен реализовать интерфейс <see cref="IAuditResponse"/>.</typeparam>
public sealed class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAuditRequest<TResponse>
    where TResponse : IAuditResponse
{
    private readonly IAuditWriter _auditWriter;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AuditBehavior{TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="auditWriter">Менеджер аудита, используемый для выполнения операций аудита.</param>
    public AuditBehavior(IAuditWriter auditWriter)
    {
        _auditWriter = auditWriter;
    }

    /// <summary>
    /// Обрабатывает запрос и добавляет аудит в пайплайн обработки запросов.
    /// </summary>
    /// <param name="request">Запрос, который будет обработан.</param>
    /// <param name="next">Делегат для выполнения следующего обработчика в пайплайне.</param>
    /// <param name="cancellationToken"> <see cref="CancellationToken"/>.</param>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        TResponse response;

        try
        {
            response = await next();

            await _auditWriter.WriteAsync(new AuditEventBaseInfo(request.EventType, request.EventObject, request.Message, true),
                cancellationToken);
        }
        catch
        {
            await _auditWriter.WriteAsync(new AuditEventBaseInfo(request.EventType, request.EventObject, request.Message, false),
                cancellationToken);

            throw;
        }

        

        return response;
    }
}
