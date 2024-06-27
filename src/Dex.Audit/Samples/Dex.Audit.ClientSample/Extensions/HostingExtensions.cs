using System.Text.Json.Serialization;
using Dex.Audit.Client.Extensions;
using Dex.Audit.Client.Messages;
using Dex.Audit.Client.Services;
using Dex.Audit.ClientSample.Repositories;
using Dex.Audit.Logger.Extensions;
using Dex.MassTransit.Rabbit;
using MassTransit;

namespace Dex.Audit.ClientSample.Extensions;

/// <summary>
/// Расширение регистрации сервисов и пайплайна
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Регистрация конфигураций сервисов
    /// </summary>
    /// <param name="builder">Web application builder</param>
    /// <returns>Web application</returns>
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var environmentName = builder.Environment.EnvironmentName;
        builder.Configuration
            .AddJsonFile("appsettings.local.json", true, true)
            .AddJsonFile($"appsettings.{environmentName}.local.json", true, true)
            .AddEnvironmentVariables();

        var services = builder.Services;

        services.AddLogging(loggingBuilder => loggingBuilder.AddAuditLogger());
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddControllers()
            .AddJsonOptions(opts =>
            {
                var enumConverter = new JsonStringEnumConverter();
                opts.JsonSerializerOptions.Converters.Add(enumConverter);
            });
        services.AddAuditClient<BaseAuditEventConfigurator, AuditSettingsRepository>(builder.Configuration);
        services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(nameof(RabbitMqOptions)));
        services.AddMassTransit(x =>
        {
            x.RegisterBus((context, configurator) =>
            {
                context.RegisterSendEndPoint<AuditEventMessage>();
                configurator.ConfigureEndpoints(context);
            });
        });

        return builder.Build();
    }

    /// <summary>
    /// Конфигурация пайплайна
    /// </summary>
    /// <param name="app">Web application</param>
    /// <returns>Web application</returns>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSwagger().UseSwaggerUI();
        app.MapControllers();
        app.MapGet(
            "/Logger", 
            (
                ILogger<Program> logger,
                LogLevel logLevel,
                string eventType,
                string message,
                string messageParameters
            ) =>
            {
                logger.LogAudit(logLevel, eventType, message, messageParameters);
            });

        return app;
    }
}