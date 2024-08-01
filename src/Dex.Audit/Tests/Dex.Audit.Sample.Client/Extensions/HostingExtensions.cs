﻿using System.Text.Json.Serialization;
using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.Client.Extensions;
using Dex.Audit.Client.Services;
using Dex.Audit.ClientSample.Commands.EFCore.AddUser;
using Dex.Audit.ClientSample.Commands.EFCore.DeleteUser;
using Dex.Audit.ClientSample.Commands.EFCore.UpdateUser;
using Dex.Audit.ClientSample.Commands.Logging;
using Dex.Audit.ClientSample.Context;
using Dex.Audit.ClientSample.Context.Interceptors;
using Dex.Audit.ClientSample.Workers;
using Dex.Audit.EF.Extensions;
using Dex.Audit.EF.Interfaces;
using Dex.Audit.Logger.Extensions;
using Dex.Audit.MediatR.PipelineBehaviours;
using Dex.Audit.Sample.Domain.Repositories;
using Dex.MassTransit.Rabbit;
using MassTransit;
using MediatR;
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

        // Client
        services.AddAuditClient<BaseAuditEventConfigurator, AuditCacheRepository>(builder.Configuration);
        services.AddAuditInterceptors<CustomInterceptionAndSendingEntriesService>();
        services.AddDbContext<ClientSampleContext>((serviceProvider, opt) =>
        {
            opt.AddInterceptors(
                serviceProvider.GetRequiredService<IAuditSaveChangesInterceptor>(),
                serviceProvider.GetRequiredService<IAuditDbTransactionInterceptor>());
        });
        services.AddLogging(loggingBuilder => loggingBuilder.AddAuditLogger(builder.Configuration));
        services.AddMassTransit(x =>
        {
            x.RegisterBus((context, configurator) =>
            {
                context.RegisterSendEndPoint<AuditEventMessage>();
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
            async (
                [FromServices] IMediator mediator,
                [FromBody] AddAuditableLogCommand request
            ) =>
            {
                await mediator.Send(request);
            });
        app.MapPost(
            "/Users", 
            async (
                [FromServices] IMediator mediator,
                [FromBody] AddUserCommand request
            ) =>
            {
                await mediator.Send(request);
            });
        app.MapPut(
            "/Users", 
            async (
                [FromServices] IMediator mediator,
                [FromBody] UpdateUserCommand request
            ) =>
            {
                await mediator.Send(request);
            });
        app.MapDelete(
            "/Users", 
            async (
                [FromServices] IMediator mediator,
                [FromBody] DeleteUserCommand request
            ) =>
            {
                await mediator.Send(request);
            });

        return app;
    }
}