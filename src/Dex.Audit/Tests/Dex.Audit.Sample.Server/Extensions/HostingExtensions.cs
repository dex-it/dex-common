using System.Text.Json.Serialization;
using Dex.Audit.Server.Extensions;
using Dex.Audit.Server.Grpc.Extensions;
using Dex.Audit.Server.Grpc.Services;
using Dex.Audit.Server.Workers;
using Dex.Audit.ServerSample.Infrastructure.Context;
using Dex.MassTransit.Rabbit;
using MassTransit;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.System.Text.Json;

namespace Dex.Audit.ServerSample.Extensions;

/// <summary>
/// Расширение регистрации сервисов и пайплайна
/// </summary>
internal static class HostingExtensions
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

        services.AddDbContext<AuditServerDbContext>();

        // Audit simple Server
        services.AddSimpleAuditServer<AuditServerDbContext, SimpleAuditServerSettingsService>(builder.Configuration);

        // // Audit Server
        // services.AddAuditServer<
        //     SimpleAuditEventsRepository<AuditServerDbContext>,
        //     SimpleAuditSettingsRepository<AuditServerDbContext>,
        //     SimpleAuditSettingsCacheRepository,
        //     SimpleAuditServerSettingsService>(builder.Configuration);
        //
        // // Audit Grpc Server
        // services.AddGrpcAuditServer<
        //     SimpleAuditEventsRepository<AuditServerDbContext>,
        //     SimpleAuditSettingsRepository<AuditServerDbContext>,
        //     SimpleAuditSettingsCacheRepository>(builder.Configuration);

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
        app.FillAuditSettings();

        app.UseSwagger().UseSwaggerUI();
        app.MapGrpcService<GrpcAuditServerSettingsService>();
        app.MapControllers();

        return app;
    }
}