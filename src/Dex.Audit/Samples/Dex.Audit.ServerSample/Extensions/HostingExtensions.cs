using System.Text.Json.Serialization;
using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;
using Dex.Audit.Server.Consumers;
using Dex.Audit.Server.Extensions;
using Dex.Audit.ServerSample.Context;
using Dex.Audit.ServerSample.Repositories;
using Dex.Audit.ServerSample.Workers;
using Dex.Extensions;
using Dex.MassTransit.Rabbit;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.System.Text.Json;

namespace Dex.Audit.ServerSample.Extensions;

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

        services.AddDbContext<AuditServerDbContext>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddControllers()
            .AddJsonOptions(opts =>
            {
                var enumConverter = new JsonStringEnumConverter();
                opts.JsonSerializerOptions.Converters.Add(enumConverter);
            });
        services.AddAuditServer<AuditRepository, AuditSettingsRepository>(builder.Configuration);
        services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(nameof(RabbitMqOptions)));
        services.AddMassTransit(busRegistrationConfigurator =>
        {
            busRegistrationConfigurator.AddConsumer<AuditEventConsumer>();

            busRegistrationConfigurator.RegisterBus((context, configurator) =>
            {
                configurator.ReceiveEndpoint(nameof(AuditEventMessage), endpointConfigurator =>
                {
                    // the transport must be configured to deliver at least the batch message limit
                    endpointConfigurator.PrefetchCount = 600;

                    endpointConfigurator.Batch<AuditEventMessage>(b =>
                    {
                        b.MessageLimit = 500;
                        b.TimeLimit = TimeSpan.FromSeconds(1);

                        b.Consumer<AuditEventConsumer, AuditEventMessage>(context);

                        b.ConcurrencyLimit = 1;
                    });

                    // retry
                    endpointConfigurator.UseMessageRetry(retryConfigurator =>
                        retryConfigurator.SetRetryPolicy(filter => filter.Interval(2, 1.Seconds())));
                });
                configurator.ConfigureEndpoints(context);
            });
        });

        AddStackExchangeRedis(services, builder.Configuration);

        services.AddHostedService<RefreshCacheWorker>();

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
            "/Settings", 
            async (AuditServerDbContext context,
                string eventType,
                AuditEventSeverityLevel severityLevel
            ) =>
            {
                context.AuditSettings.Add(new AuditSettings() { EventType = eventType, SeverityLevel = severityLevel });
                await context.SaveChangesAsync();
            });
        app.MapGet(
            "/Events", 
            async (AuditServerDbContext context
            ) => await context.AuditEvents.ToListAsync());


        return app;
    }
}