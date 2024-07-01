using System.Text.Json.Serialization;
using Dex.Audit.Server.Consumers;
using Dex.Audit.Server.Extensions;
using Dex.Audit.ServerSample.Context;
using Dex.Audit.ServerSample.Repositories;
using Dex.MassTransit.Rabbit;
using MassTransit;
using MassTransit.Configuration;

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

        services.AddDistributedMemoryCache();
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
        services.AddMassTransit(x =>
        {
            x.RegisterConsumer<AuditEventConsumer>();

            x.RegisterBus((context, configurator) =>
            {
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

        return app;
    }
}