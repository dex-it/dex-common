using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.Outbox.Ef
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        public static IServiceCollection AddOutbox<TDbContext>(this IServiceCollection serviceProvider) where TDbContext : DbContext
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            
            return serviceProvider.AddScoped<IOutboxService, OutboxService<TDbContext>>()
                .AddScoped<IOutboxHandler, OutboxHandler<TDbContext>>()
                .AddScoped<IOutboxSerializer, DefaultOutboxSerializer>()
                .AddScoped<IOutboxDataProvider<TDbContext>, EfOutboxDataProvider<TDbContext>>()
                .AddScoped<IOutboxMessageHandlerFactory, OutboxMessageHandlerFactory>();
        }
    }
}