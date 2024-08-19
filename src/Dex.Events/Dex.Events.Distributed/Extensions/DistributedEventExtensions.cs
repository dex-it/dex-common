using System;
using System.Linq;
using System.Reflection;
using Dex.Extensions;
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
        /// <param name="assembly">Assembly for IDistributedEventHandler</param>
        public static void RegisterAllEventHandlers(
            this IBusRegistrationConfigurator registration,
            Assembly? assembly = null)
        {
            assembly = assembly == null ? Assembly.GetCallingAssembly() : assembly;
            assembly.GetTypes()
                .Where(type =>
                    typeof(IDistributedEventHandler).IsAssignableFrom(type) &&
                    type is { IsAbstract: false, IsInterface: false })
                .ForEach(x => registration.AddConsumer(x));
        }

        /// <summary>
        /// Register IDistributedEventHandle and configure it 
        /// </summary>
        /// <param name="registration">Bus consumers registration context</param>
        /// <param name="configurator"></param>
        public static void RegisterEventHandler<T>(
            this IBusRegistrationConfigurator registration,
            Action<IConsumerConfigurator<T>>? configurator = null)
            where T : class, IDistributedEventHandler
        {
            if (registration == null) throw new ArgumentNullException(nameof(registration));
            if (configurator != null)
            {
                registration.AddConsumer(configurator);
            }
            else
            {
                registration.AddConsumer<T>();
            }
        }
    }
}