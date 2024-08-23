using System.Text.Json.Serialization;
using Dex.Audit.Client.Extensions;
using Dex.Audit.Client.Grpc.Extensions;
using Dex.Audit.Client.Services;
using Dex.Audit.ClientSample.Infrastructure.Consumers;
using Dex.Audit.ClientSample.Infrastructure.Context;
using Dex.Audit.ClientSample.Infrastructure.Context.Interceptors;
using Dex.Audit.ClientSample.Infrastructure.Workers;
using Dex.Audit.EF.Extensions;
using Dex.Audit.EF.Interfaces;
using Dex.Audit.Logger.Extensions;
using Dex.Audit.MediatR.PipelineBehaviours;
using Dex.Audit.Sample.Shared.Dto;
using Dex.MassTransit.Rabbit;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using AuditCacheRepository = Dex.Audit.ClientSample.Infrastructure.Repositories.AuditCacheRepository;

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
        services.AddScoped<IDistributedCache, MemoryDistributedCache>();

        // Client
        // services.AddAuditClient<BaseAuditEventConfigurator, AuditCacheRepository, ClientAuditSettingsService>(builder.Configuration);

        // Grpc client
        services.AddGrpcAuditClient<BaseAuditEventConfigurator, AuditCacheRepository>(
            builder.Configuration, 
            () =>
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                return handler;
            });

        services.AddAuditInterceptors<CustomInterceptionAndSendingEntriesService>();
        services.AddDbContext<ClientSampleContext>((serviceProvider, opt) =>
        {
            opt.AddInterceptors(
                serviceProvider.GetRequiredService<IAuditSaveChangesInterceptor>(),
                serviceProvider.GetRequiredService<IAuditDbTransactionInterceptor>());
        });
        services.AddLogging(loggingBuilder => loggingBuilder.AddAuditLogger());
        services.AddMassTransit(x =>
        {
            x.AddConsumer<AuditSettingsUpdatedConsumer>();

            x.RegisterBus((context, configurator) =>
            {
                context.RegisterReceiveEndpoint<AuditSettingsUpdatedConsumer, AuditSettingsDto>(configurator);
                context.AddAuditClientSendEndpoint();
                configurator.ConfigureEndpoints(context);
            });
        });
        services.AddTransient(typeof(AuditBehavior<,>), typeof(AuditBehavior<,>));
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
            configuration.AddBehavior(typeof(IPipelineBehavior<,>),typeof(AuditBehavior<,>));
        });

        // Additional
        services.AddHostedService<SubsystemAuditWorker>();

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