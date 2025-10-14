using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit.Extensions;

public static class MicrosoftDependencyInjectionExtensions
{
    public static IServiceCollection AddOutboxPublisher(this IServiceCollection serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        serviceProvider.AddScoped(typeof(IOutboxMessageHandler<>), typeof(PublisherOutboxHandler<>));
        return serviceProvider;
    }
}