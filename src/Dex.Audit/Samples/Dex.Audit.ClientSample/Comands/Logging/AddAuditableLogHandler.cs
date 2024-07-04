using Dex.Audit.Logger.Extensions;
using MediatR;

namespace Dex.Audit.ClientSample.Comands.Logging;

public class AddAuditableLogHandler(ILogger<AddAuditableLogHandler> logger) : IRequestHandler<AddAuditableLogCommand, AddAuditableLogResponse>
{
    public Task<AddAuditableLogResponse> Handle(AddAuditableLogCommand request, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogAudit(request.LogLevel, request.LogEventName, request.LogMessage, request.LogMessageParams);
            return Task.FromResult(new AddAuditableLogResponse(true));
        }
        catch (Exception exception)
        {
            logger.LogAudit(LogLevel.Error, request.LogEventName, exception, exception.Message);
            return Task.FromResult(new AddAuditableLogResponse(true));
        }
    }
}