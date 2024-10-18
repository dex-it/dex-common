using System.Threading.Channels;
using Dex.Audit.Client.Abstractions.Messages;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Logger;

/// <summary>
/// Logger, for audio logs.
/// </summary>
internal class AuditLogger : ILogger
{
    internal static readonly Channel<AuditEventBaseInfo> BaseInfoChannel =
        Channel.CreateBounded<AuditEventBaseInfo>(new BoundedChannelOptions(int.MaxValue));

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (eventId.Id != AuditLoggerConstants.AuditEventId || string.IsNullOrEmpty(eventId.Name)) return;

        var success = logLevel is not (LogLevel.Critical or LogLevel.Error or LogLevel.Warning);

        BaseInfoChannel.Writer.TryWrite(new AuditEventBaseInfo(
            eventId.Name,
            nameof(Log),
            formatter(state, exception),
            success));
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