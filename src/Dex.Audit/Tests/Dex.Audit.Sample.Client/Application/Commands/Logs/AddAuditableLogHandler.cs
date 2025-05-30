﻿using System.Text.Json.Serialization;
using Dex.Audit.Logger.Extensions;
using Dex.Audit.MediatR.Requests;
using Dex.Audit.MediatR.Responses;
using Dex.Audit.Sample.Shared.Enums;
using MediatR;

namespace Dex.Audit.Sample.Client.Application.Commands.Logs;

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

public sealed class AddAuditableLogCommand : AuditRequest<AddAuditableLogResponse>
{
    public override string EventType { get; } = AuditEventType.ObjectCreated.ToString();
    public override string EventObject { get; } = nameof(AddAuditableLogCommand);
    public override string Message { get; } = "Auditable log added";

    public required string LogEventName { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogLevel LogLevel { get; set; }

    public string? LogMessage { get; set; }

    public string? LogMessageParams { get; set; }
}

public sealed record AddAuditableLogResponse(bool Result) : IAuditResponse;