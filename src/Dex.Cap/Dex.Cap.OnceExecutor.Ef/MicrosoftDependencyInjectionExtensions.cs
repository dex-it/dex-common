using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Cap.OnceExecutor.Ef
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        public static IServiceCollection AddOnceExecutor<TDbContext>(this IServiceCollection serviceProvider) where TDbContext : DbContext
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

            return serviceProvider
                    .AddScoped(typeof(IOnceExecutor<TDbContext>), typeof(OnceExecutorEf<TDbContext>))
                ;
        }
    }
}