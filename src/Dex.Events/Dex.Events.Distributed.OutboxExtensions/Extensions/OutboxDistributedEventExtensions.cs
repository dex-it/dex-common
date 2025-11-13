using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Events.Distributed.OutboxExtensions.Extensions;

public static class OutboxDistributedEventExtensions
{
    public static IServiceCollection RegisterOutboxDistributedEventHandler(this IServiceCollection services)
    {
        return services.AddScoped<IOutboxMessageHandler<OutboxDistributedEventMessage>, OutboxDistributedEventHandler>();
    }
}