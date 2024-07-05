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
/// <param name="auditWriter">Менеджер аудита, используемый для выполнения операций аудита.</param>
public sealed class AuditBehavior<TRequest, TResponse>(IAuditWriter auditWriter) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAuditRequest<TResponse>
    where TResponse : IAuditResponse
{
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
            response = await next().ConfigureAwait(false);

            await auditWriter.WriteAsync(new AuditEventBaseInfo(request.EventType, request.EventObject, request.Message, true),
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await auditWriter.WriteAsync(new AuditEventBaseInfo(request.EventType, request.EventObject, request.Message, false),
                cancellationToken).ConfigureAwait(false);

            throw;
        }

        return response;
    }
}
