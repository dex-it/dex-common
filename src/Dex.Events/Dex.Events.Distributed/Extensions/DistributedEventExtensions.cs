using System.Reflection;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Events.Distributed.Extensions
{
    public static class DistributedEventExtensions
    {
        public static IServiceCollection RegisterDistributedEventRaiser(this IServiceCollection services)
            => services.AddScoped(typeof(IDistributedEventRaiser<>), typeof(DistributedEventRaiser<>));

        /// <summary>
        /// Register all IDistributedEventHandler, when assembly is null, GetCallingAssembly is called
        /// </summary>
        /// <param name="registration">Bus consumers registration context</param>
        /// <param name="assembly">Assemply for IDistributedEventHandler</param>
        public static void RegisterAllEventHandlers(this IBusRegistrationConfigurator registration, Assembly? assembly = null)
        {
            assembly = assembly == null ? Assembly.GetCallingAssembly() : assembly;
            registration.AddConsumers(type => typeof(IDistributedEventHandler).IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface, assembly);
        }
    }
}