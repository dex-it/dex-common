using Dex.Audit.Contracts.Messages;
using Dex.Audit.Writer.Consumers;
using Dex.Audit.Writer.Helpers;
using Dex.MassTransit.Rabbit;
using MassTransit;

namespace Dex.Audit.Writer.Extensions;

/// <summary>
/// Статический класс, который содержит методы расширения для конфигурации зависимостей
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет конфигурацию для работы с RabbitMQ (MassTransit)
    /// </summary>
    public static void AddMassTransit(this IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            x.AddDelayedMessageScheduler();

            x.AddConsumer<AuditEventConsumer>(configurator =>
            {
                configurator.UseDefaultConfiguration(TransientExceptions.Check);
            });

            x.RegisterBus((context, configurator) =>
            {
                configurator.UseDelayedMessageScheduler();

                context.RegisterReceiveEndpoint<AuditEventConsumer, AuditEventMessage>(configurator);
            });
        });
    }

    /// <summary>
    /// Добавит автомапер со сборками
    /// </summary>
    public static void AddAutoMapperWithAssemblies(this IServiceCollection services)
    {
        services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
    }
}
