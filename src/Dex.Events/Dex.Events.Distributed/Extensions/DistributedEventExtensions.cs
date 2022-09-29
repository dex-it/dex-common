using Microsoft.Extensions.DependencyInjection;

namespace Dex.Events.Distributed.Extensions
{
    public static class DistributedEventExtensions
    {
        public static IServiceCollection RegisterDistributedEventRaiser(this IServiceCollection services)
            => services.AddScoped(typeof(IDistributedEventRaiser<>), typeof(DistributedEventRaiser<>));
    }
}