using System;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.Outbox.Ef
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection serviceProvider) where TDbContext : DbContext
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            serviceProvider.AddHealthChecks()
                .AddCheck<OutboxHealthCheck<TDbContext>>("outbox-scheduler");

            return serviceProvider
                .AddScoped<IOutboxService<TDbContext>, OutboxService<TDbContext>>()
                .AddScoped<IOutboxService>(provider => provider.GetRequiredService<IOutboxService<TDbContext>>())
                .AddScoped<IOutboxHandler, OutboxHandler<TDbContext>>()
                .AddScoped<IOutboxSerializer, DefaultOutboxSerializer>()
                .AddScoped<IOutboxDataProvider<TDbContext>, OutboxDataProviderEf<TDbContext>>()
                .AddScoped<IOutboxCleanupDataProvider, OutboxCleanupDataProviderEf<TDbContext>>()
                .AddScoped<IOutboxMessageHandlerFactory, OutboxMessageHandlerFactory>();
        }
    }
}