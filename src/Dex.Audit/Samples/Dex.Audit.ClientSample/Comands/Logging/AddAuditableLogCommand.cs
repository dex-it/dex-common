using System.Text.Json.Serialization;
using Dex.Audit.MediatR.Requests;

namespace Dex.Audit.ClientSample.Comands.Logging;

public class AddAuditableLogCommand : IAuditRequest<AddAuditableLogResponse>
{
    [JsonIgnore]
    public string EventType { get; } = nameof(AddAuditableLogCommand);

    [JsonIgnore]
    public string EventObject { get; } = nameof(AddAuditableLogCommand);

    [JsonIgnore]
    public string Message { get; } = "Auditable log added";

    public required string LogEventName { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogLevel LogLevel { get; set; }

    public string? LogMessage { get; set; }

    public string? LogMessageParams { get; set; }
}