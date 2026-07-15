using System;
using System.Linq;
using Dex.Cap.Inbox.AspNetScheduler;
using Dex.Cap.Inbox.AspNetScheduler.BackgroundServices;
using Dex.Cap.Inbox.AspNetScheduler.Interfaces;
using Dex.Cap.Inbox.AspNetScheduler.Options;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Options;
using Dex.Cap.Inbox.RetryStrategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox.Ef.Extensions;

/// <summary>
/// Регистрация инбокса и его фоновых сервисов в контейнере.
/// </summary>
public static class MicrosoftDependencyInjectionExtensions
{
    /// <summary>
    /// Маркер, чтобы health check регистрировался один раз даже при нескольких вызовах AddInboxScheduler.
    /// </summary>
    private sealed class InboxHealthCheckRegistered;

    /// <summary>
    /// Зарегистрировать инбокс и EF-провайдер данных.
    /// </summary>
    public static IServiceCollection AddInbox<TDbContext>(
        this IServiceCollection services,
        Action<IServiceProvider, InboxRetryStrategyConfigurator>? retryStrategyImplementation = null)
        where TDbContext : DbContext
    {
        return services.AddInbox<TDbContext>(_ => { }, retryStrategyImplementation);
    }

    /// <summary>
    /// Зарегистрировать инбокс и EF-провайдер данных.
    /// </summary>
    /// <remarks>
    /// Опции валидируются на старте хоста: неверная конфигурация должна падать при запуске, а не
    /// проявляться поведением на проде. Там же строится реестр типов сообщений: коллизия дискриминаторов
    /// обязана ронять старт, а не всплывать позже внутри фонового обработчика, где она превратилась бы в
    /// LogCritical при формально поднятом хосте.
    /// <para>
    /// Синглтоны регистрируются через TryAdd: повторный вызов иначе оставляет в коллекции лишние
    /// дескрипторы, а регистрация потребителя, сделанная ДО этого вызова, молча проигрывала бы дефолтной.
    /// Scoped-сервисы, включая <see cref="IInboxSerializer"/>, регистрируются обычным Add, поэтому
    /// побеждает регистрация потребителя, сделанная ПОСЛЕ.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddInbox<TDbContext>(
        this IServiceCollection services,
        Action<InboxOptions> configure,
        Action<IServiceProvider, InboxRetryStrategyConfigurator>? retryStrategyImplementation = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<InboxOptions>()
            .Configure(configure)
            .ValidateOnStart();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<InboxOptions>, InboxOptionsValidator>());

        services.TryAddSingleton<IInboxMetricCollector, DefaultInboxMetricCollector>();
        services.TryAddSingleton<IInboxMessageTypeSource, AppDomainInboxMessageTypeSource>();
        services.TryAddSingleton<IInboxTypeDiscriminatorProvider, InboxTypeDiscriminatorProvider>();
        services.TryAddSingleton<IInboxStatistic>(provider => provider.GetRequiredService<IInboxMetricCollector>());

        services.AddHostedService<InboxRegistryWarmupService>();

        services.AddScoped<IInboxService, InboxService>();
        services.AddScoped<IInboxEnvelopFactory, InboxEnvelopFactory>();
        services.AddScoped<IInboxHandler, MainLoopInboxHandler>();
        services.AddScoped<IInboxJobHandler, InboxJobHandlerEf<TDbContext>>();
        services.AddScoped<IInboxSerializer, DefaultInboxSerializer>();
        services.AddScoped<IInboxDataProvider, InboxDataProviderEf<TDbContext>>();
        services.AddScoped<IInboxMessageHandlerFactory, InboxMessageHandlerFactory>();

        services.AddScoped<IInboxRetryStrategy>(provider =>
        {
            var configurator = new InboxRetryStrategyConfigurator();
            retryStrategyImplementation?.Invoke(provider, configurator);
            return configurator.RetryStrategy;
        });

        return services;
    }

    /// <summary>
    /// Зарегистрировать фоновую обработку и чистку инбокса с EF-провайдером чистки.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="periodSeconds">Пауза между циклами, когда очередь исчерпана.</param>
    /// <param name="cleanupDays">
    /// Ретеншен обработанных сообщений. Он же окно дедупликации: повторная доставка,
    /// пришедшая позже этого срока, будет принята как новое сообщение.
    /// </param>
    public static IServiceCollection AddDefaultInboxScheduler<TDbContext>(
        this IServiceCollection services, int periodSeconds = 30, int cleanupDays = 30)
        where TDbContext : DbContext
    {
        return AddInboxScheduler<InboxCleanupDataProviderEf<TDbContext>>(services, periodSeconds, cleanupDays);
    }

    /// <summary>
    /// Зарегистрировать фоновую обработку и чистку инбокса с EF-провайдером чистки.
    /// </summary>
    public static IServiceCollection AddDefaultInboxScheduler<TDbContext>(
        this IServiceCollection services, Action<InboxHandlerOptions> configure)
        where TDbContext : DbContext
    {
        return AddInboxScheduler<InboxCleanupDataProviderEf<TDbContext>>(services, configure);
    }

    /// <summary>
    /// Зарегистрировать фоновую обработку и чистку инбокса.
    /// </summary>
    public static IServiceCollection AddInboxScheduler<TCleanUpDataProvider>(
        this IServiceCollection services, int periodSeconds = 30, int cleanupDays = 30)
        where TCleanUpDataProvider : class, IInboxCleanupDataProvider
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(periodSeconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cleanupDays);

        return AddInboxScheduler<TCleanUpDataProvider>(services, options =>
        {
            options.Period = TimeSpan.FromSeconds(periodSeconds);
            options.CleanupOlderThan = TimeSpan.FromDays(cleanupDays);
            options.CleanupInterval = TimeSpan.FromHours(1);
        });
    }

    /// <summary>
    /// Зарегистрировать фоновую обработку и чистку инбокса.
    /// </summary>
    public static IServiceCollection AddInboxScheduler<TCleanUpDataProvider>(
        this IServiceCollection services, Action<InboxHandlerOptions> configure)
        where TCleanUpDataProvider : class, IInboxCleanupDataProvider
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        if (services.All(d => d.ServiceType != typeof(InboxHealthCheckRegistered)))
        {
            services.AddSingleton<InboxHealthCheckRegistered>();
            services.AddHealthChecks().AddCheck<InboxHealthCheck>("inbox-scheduler", tags: ["inbox-scheduler"]);
        }

        services.AddOptions<InboxHandlerOptions>()
            .Configure(configure)
            .ValidateOnStart();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<InboxHandlerOptions>, InboxHandlerOptionsValidator>());

        services.AddScoped<IInboxCleanupDataProvider, TCleanUpDataProvider>();
        services.AddScoped<IInboxCleanerHandler, InboxCleanerHandler>();

        services.AddHostedService<InboxHandlerBackgroundService>();
        services.AddHostedService<InboxCleanerBackgroundService>();

        return services;
    }
}
