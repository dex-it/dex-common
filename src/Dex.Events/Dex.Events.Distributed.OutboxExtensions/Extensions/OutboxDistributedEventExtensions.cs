﻿using Dex.Cap.Outbox.Interfaces;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Events.Distributed.OutboxExtensions.Extensions
{
    public static class OutboxDistributedEventExtensions
    {
        public static IServiceCollection RegisterOutboxDistributedEventHandler(this IServiceCollection services)
            => services.RegisterOutboxDistributedEventHandler<IBus>();

        public static IServiceCollection RegisterOutboxDistributedEventHandler<TBus>(this IServiceCollection services)
            where TBus : IBus
            => services.AddScoped<IOutboxMessageHandler<OutboxDistributedEventMessage<TBus>>, OutboxDistributedEventHandler<TBus>>();
    }
}