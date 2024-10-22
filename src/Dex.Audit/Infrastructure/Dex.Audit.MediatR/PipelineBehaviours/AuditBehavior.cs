using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.MediatR.Requests;
using Dex.Audit.MediatR.Responses;
using MediatR;

namespace Dex.Audit.MediatR.PipelineBehaviours;

/// <summary>
/// Represents the audit behavior for use in request processing pipelines.
/// </summary>
/// <typeparam name="TRequest">Type of the request that must implement the <see cref="AuditRequest{TResponse}"/> interface.</typeparam>
/// <typeparam name="TResponse">Type of the response that must implement the <see cref="IAuditResponse"/> interface.</typeparam>
/// <param name="auditWriter">The audit manager used to perform audit operations.</param>
internal sealed class AuditBehavior<TRequest, TResponse>(
    IAuditWriter auditWriter)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : AuditRequest<TResponse>
    where TResponse : IAuditResponse
{
    /// <summary>
    /// Processes the request and adds audit to the request processing pipeline.
    /// </summary>
    /// <param name="request">The request to be processed.</param>
    /// <param name="next">Delegate to execute the next handler in the pipeline.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        TResponse response;

        try
        {
            // Process the request and get the response
            response = await next().ConfigureAwait(false);

            // Write audit log for successful operation
            await WriteAuditAsync(request, true, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            // Write audit log for failed operation
            await WriteAuditAsync(request, false, cancellationToken)
                .ConfigureAwait(false);

            throw; // Re-throw the exception
        }

        return response;
    }

    /// <summary>
    /// Writes the audit log.
    /// </summary>
    /// <param name="request">The audit request.</param>
    /// <param name="success">Indicates whether the request was successful.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private Task WriteAuditAsync(
        AuditRequest<TResponse> request,
        bool success,
        CancellationToken cancellationToken)
    {
        // Log the audit event based on the request details
        return auditWriter
            .WriteAsync(
                new AuditEventBaseInfo(request.EventType, request.EventObject, request.Message, success),
                cancellationToken);
    }
}
