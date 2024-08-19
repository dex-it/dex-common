using System.Text.Json.Serialization;
using Dex.Audit.Domain.Entities;
using Dex.Audit.Domain.Enums;
using Dex.Audit.Sample.Shared.Enums;
using Dex.Audit.Server.Abstractions.Interfaces;
using Dex.Audit.Server.Extensions;
using Dex.Audit.Server.Grpc.Services;
using Dex.Audit.ServerSample.Application.Services;
using Dex.Audit.ServerSample.Infrastructure.Context;
using Dex.Audit.ServerSample.Infrastructure.Repositories;
using Dex.Audit.ServerSample.Infrastructure.Workers;
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
        services.AddAuditServer<AuditPersistentRepository, AuditCacheRepository, AuditServerSettingsService>(builder.Configuration);

        // Grpc Server
        //services.AddGrpcAuditServer<AuditPersistentRepository, AuditCacheRepository>(builder.Configuration);

        services.AddMassTransit(busRegistrationConfigurator =>
        {
            busRegistrationConfigurator.AddAuditServerConsumer();

            busRegistrationConfigurator.RegisterBus((context, configurator) =>
            {
                configurator.AddAuditServerReceiveEndpoint(context, true);
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
                await settingsServer.AddOrUpdateSettingsAsync(eventType, severityLevel);
            });
        app.MapDelete("/Settings",
            async (IAuditServerSettingsService settingsServer, string eventType) =>
            {
                await settingsServer.DeleteSettingsAsync(eventType);
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