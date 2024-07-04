using System.Text.Json.Serialization;
using Dex.Audit.Client.Extensions;
using Dex.Audit.Client.Messages;
using Dex.Audit.Client.Services;
using Dex.Audit.ClientSample.Models;
using Dex.Audit.ClientSample.Repositories;
using Dex.Audit.Logger.Extensions;
using Dex.MassTransit.Rabbit;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.System.Text.Json;

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

        services.AddLogging(loggingBuilder => loggingBuilder.AddAuditLogger(builder.Configuration));
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

        AddStackExchangeRedis(services, builder.Configuration);

        return builder.Build();
    }

    private static void AddStackExchangeRedis(IServiceCollection services, ConfigurationManager builderConfiguration)
    {
        var redisConfiguration = new RedisConfiguration();
        builderConfiguration.GetSection(nameof(RedisConfiguration)).Bind(redisConfiguration);
        services.AddStackExchangeRedisExtensions<SystemTextJsonSerializer>(redisConfiguration);
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
        app.MapPost(
            "/Logger", 
            (
                [FromServices] ILogger<Program> logger,
                [FromBody] CreateLoggableEvent loggableEvent
            ) =>
            {
                logger.LogAudit(loggableEvent.LogLevel, loggableEvent.EventType, loggableEvent.Message, loggableEvent.MessageParameters);
            });

        return app;
    }
}