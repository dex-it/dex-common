using System.Text.Json.Serialization;

namespace Dex.Audit.ClientSample.Models;

public class CreateLoggableEvent
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogLevel LogLevel { get; set; }
    public required string EventType { get; set; }
    public string? Message { get; set; }
    public string? MessageParameters { get; set; }
}