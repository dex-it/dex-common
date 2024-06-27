using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Channels;
using Dex.Audit.Client.Messages;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Logger;

internal class AuditLogger : ILogger
{
    private readonly JsonSerializerOptions _options = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
        WriteIndented = true
    };

    internal static readonly Channel<AuditEventBaseInfo> BaseInfoChannel = Channel.CreateBounded<AuditEventBaseInfo>(new BoundedChannelOptions(Int32.MaxValue));

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (eventId.Id != int.MaxValue || string.IsNullOrEmpty(eventId.Name)) return;

        bool failure = logLevel is LogLevel.Critical or LogLevel.Error or LogLevel.Warning;

        BaseInfoChannel.Writer.TryWrite(new AuditEventBaseInfo(
            eventId.Name,
            JsonSerializer.Serialize(state, _options),
            formatter(state, exception),
            !failure));
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }
}