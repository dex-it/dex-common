using Dex.Audit.Logger.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Logger.Extensions;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Добавить аудируемые логи.
    /// </summary>
    /// <param name="builder"><see cref="ILoggingBuilder"/>.</param>
    /// <param name="dispose">Освобождать ли ресурсы средствами DI.</param>
    /// <returns></returns>
    public static ILoggingBuilder AddAuditLogger(
        this ILoggingBuilder builder,
        bool dispose = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var services = builder.Services;

        if (dispose)
        {
            services.AddSingleton<ILoggerProvider, AuditLoggerProvider>(_ => new AuditLoggerProvider());
        }
        else
        {
            builder.AddProvider(new AuditLoggerProvider());
        }

        builder.AddFilter<AuditLoggerProvider>(_ => true).SetMinimumLevel(LogLevel.Trace);

        services.AddHostedService<AuditLoggerReader>();

        services.AddOptions<AuditLoggerOptions>(nameof(AuditLoggerOptions));

        return builder;
    }
}