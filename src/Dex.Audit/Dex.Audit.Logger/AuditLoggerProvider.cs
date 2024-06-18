using Microsoft.Extensions.Logging;

namespace Dex.Audit.Logger;

public class AuditLoggerProvider : ILoggerProvider
{
    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new AuditLogger();
    }
}