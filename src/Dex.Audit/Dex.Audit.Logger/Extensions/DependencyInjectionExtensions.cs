using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Logger.Extensions;

public static class DependencyInjectionExtensions
{
    public static ILoggingBuilder AddAuditLogger(this ILoggingBuilder builder, bool dispose = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (dispose)
        {
            builder.Services.AddSingleton<ILoggerProvider, AuditLoggerProvider>(_ => new AuditLoggerProvider());
        }
        else
        {
            builder.AddProvider(new AuditLoggerProvider());
        }

        builder.Services.AddHostedService<AuditLoggerReader>();

        return builder;
    }
}