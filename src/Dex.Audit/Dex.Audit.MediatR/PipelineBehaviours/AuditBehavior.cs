using Dex.Audit.Client.Interfaces;
using Dex.Audit.Client.Messages;
using Dex.Audit.MediatR.Requests;
using Dex.Audit.MediatR.Responses;
using MediatR;

namespace Dex.Audit.MediatR.PipelineBehaviours;

/// <summary>
/// Представляет поведение аудита для использования в пайплайнах обработки запросов.
/// </summary>
/// <typeparam name="TRequest">Тип запроса, который должен реализовать интерфейс <see cref="AuditRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">Тип ответа, который должен реализовать интерфейс <see cref="IAuditResponse"/>.</typeparam>
/// <param name="auditWriter">Менеджер аудита, используемый для выполнения операций аудита.</param>
public sealed class AuditBehavior<TRequest, TResponse>(IAuditWriter auditWriter) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : AuditRequest<TResponse>
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

            await WriteAuditAsync(request, true, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await WriteAuditAsync(request, false, cancellationToken).ConfigureAwait(false);

            throw;
        }

        return response;
    }

    private async Task WriteAuditAsync(AuditRequest<TResponse> request, bool success, CancellationToken cancellationToken)
    {
        await auditWriter.WriteAsync(
            new AuditEventBaseInfo(request.EventType, request.EventObject, request.Message, success),
            cancellationToken).ConfigureAwait(false);
    }
}
