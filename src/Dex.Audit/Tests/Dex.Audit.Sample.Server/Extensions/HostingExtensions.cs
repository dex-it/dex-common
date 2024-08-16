using System.Text.Json.Serialization;
using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;
using Dex.Audit.Sample.Shared.Enums;
using Dex.Audit.Sample.Shared.Repositories;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.Server.Consumers;
using Dex.Audit.Server.Grpc.Extensions;
using Dex.Audit.Server.Grpc.Services;
using Dex.Audit.Server.Workers;
using Dex.Audit.ServerSample.Infrastructure.Context;
using Dex.Audit.ServerSample.Infrastructure.Repositories;
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
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenLocalhost(7240, listenOptions =>
            {
                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
                listenOptions.UseHttps();
            });
        });
        // Base
        var environmentName = builder.Environment.EnvironmentName;
        builder.Configuration
            .AddJsonFile("appsettings.local.json", true, true)
            .AddJsonFile($"appsettings.{environmentName}.local.json", true, true)
            .AddEnvironmentVariables();

        var services = builder.Services;

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddControllers()
            .AddJsonOptions(opts =>
            {
                var enumConverter = new JsonStringEnumConverter();
                opts.JsonSerializerOptions.Converters.Add(enumConverter);
            });
        services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(nameof(RabbitMqOptions)));
        AddStackExchangeRedis(services, builder.Configuration);

        // Server
        services.AddDbContext<AuditServerDbContext>();
        //services.AddAuditServer<AuditPersistentRepository, AuditCacheRepository>(builder.Configuration);
        services.AddGrpcAuditServer<AuditPersistentRepository, AuditCacheRepository>(builder.Configuration);
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

        // Additional
        services.AddHostedService<RefreshCacheWorker>();

        services.AddGrpc();

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
        FillAuditSettings(app);

        app.UseSwagger().UseSwaggerUI();
        app.MapGrpcService<GrpcAuditServerSettingsService>();
        app.MapControllers();
        app.MapGet(
            "/Settings", 
            async (AuditServerDbContext context
            ) => await context.AuditSettings.ToListAsync());
        app.MapGet(
            "/Events", 
            async (AuditServerDbContext context
            ) => await context.AuditEvents.ToListAsync());
        
        app.MapPut("/Settings",
            async (IAuditServerSettingsService settingsServer, string eventType, AuditEventSeverityLevel severityLevel) =>
            {
                await settingsServer.AddOrUpdateSettings(eventType, severityLevel);
            });
        app.MapDelete("/Settings",
            async (IAuditServerSettingsService settingsServer, string eventType) =>
            {
                await settingsServer.DeleteSettings(eventType);
            });

        return app;
    }

    private static void FillAuditSettings(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<AuditServerDbContext>();
        var settings = new AuditSettings
            []
            {
                new()
                {
                    EventType = AuditEventType.None.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.First
                },
                new()
                {
                    EventType = AuditEventType.StartSystem.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.First
                },
                new()
                {
                    EventType = AuditEventType.ShutdownSystem.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.First
                },
                new()
                {
                    EventType = AuditEventType.ObjectCreated.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.First
                },
                new()
                {
                    EventType = AuditEventType.ObjectChanged.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.First
                },
                new()
                {
                    EventType = AuditEventType.ObjectRead.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.First
                },
                new()
                {
                    EventType = AuditEventType.ObjectDeleted.ToString(),
                    SeverityLevel = AuditEventSeverityLevel.First
                }
            };
        context.AuditSettings.AddRange(settings);
        context.SaveChanges();
    }
}