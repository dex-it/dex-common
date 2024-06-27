using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Logger;

public class AuditLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, AuditLogger> _loggers = new();

    public void Dispose()
    {
        _loggers.Clear();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, new AuditLogger());
    }
}