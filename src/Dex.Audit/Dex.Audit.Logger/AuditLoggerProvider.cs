using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Logger;

public class AuditLoggerProvider : ILoggerProvider
{
    private bool _disposedValue;
    private readonly ConcurrentDictionary<string, AuditLogger> _loggers = new();

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, new AuditLogger());
    }

    ~AuditLoggerProvider() => Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            _loggers.Clear();
        }

        _disposedValue = true;
    }
}