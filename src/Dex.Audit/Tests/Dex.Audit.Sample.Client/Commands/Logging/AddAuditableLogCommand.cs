using System.Text.Json.Serialization;
using Dex.Audit.MediatR.Requests;
using Dex.Audit.Sample.Domain.Enums;

namespace Dex.Audit.ClientSample.Commands.Logging;

public class AddAuditableLogCommand : AuditRequest<AddAuditableLogResponse>
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