using Dex.Audit.Logger.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Logger.Extensions;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Добавить аудируемые логи.
    /// </summary>
    /// <param name="builder"><see cref="ILoggingBuilder"/>.</param>
    /// <param name="configuration"><see cref="IConfiguration"/></param>
    /// <param name="dispose">Освобождать ли ресурсы средствами DI.</param>
    /// <returns></returns>
    public static ILoggingBuilder AddAuditLogger(
        this ILoggingBuilder builder,
        IConfiguration configuration,
        bool dispose = false)
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

        builder.AddFilter<AuditLoggerProvider>(_ => true).SetMinimumLevel(LogLevel.Trace);

        builder.Services.AddHostedService<AuditLoggerReader>();
        builder.Services.Configure<AuditLoggerOptions>(opts =>
            configuration.GetSection(nameof(AuditLoggerOptions)).Bind(opts));

        return builder;
    }
}