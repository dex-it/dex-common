using System;
using System.Reflection;
using Dex.Events.Distributed.Models;
using MassTransit;

namespace Dex.Events.Distributed.Extensions
{
    public static class MassTransitExtensions
    {
        /// <summary>
        /// Register template consumer
        /// </summary>
        /// <param name="configurator">IBusRegistrationConfigurator</param>
        /// <param name="registrationContext">IBusRegistrationContext</param>
        /// <typeparam name="T">DistributedBaseEventParams</typeparam>
        /// <typeparam name="TH1"></typeparam>
        public static void RegisterDistributedEventHandlers<T, TH1>(this IReceiveConfigurator<IReceiveEndpointConfigurator> configurator,
            IBusRegistrationContext registrationContext)
            where T : DistributedBaseEventParams
            where TH1 : IDistributedEventHandler<T>
        {
            if (configurator == null) throw new ArgumentNullException(nameof(configurator));
            if (registrationContext == null) throw new ArgumentNullException(nameof(registrationContext));

            var serviceName = Assembly.GetCallingAssembly().GetName().Name;
            configurator.ReceiveEndpoint(serviceName + "." + typeof(TH1).Name, x => x.ConfigureConsumer(registrationContext, typeof(TH1)));
        }

        /// <summary>
        /// Register template consumer
        /// </summary>
        /// <param name="configurator">IBusRegistrationConfigurator</param>
        /// <param name="registrationContext">IBusRegistrationContext</param>
        /// <typeparam name="T">DistributedBaseEventParams</typeparam>
        /// <typeparam name="TH1"></typeparam>
        /// <typeparam name="TH2"></typeparam>
        public static void RegisterDistributedEventHandlers<T, TH1, TH2>(this IReceiveConfigurator<IReceiveEndpointConfigurator> configurator,
            IBusRegistrationContext registrationContext)
            where T : DistributedBaseEventParams
            where TH1 : IDistributedEventHandler<T>
            where TH2 : IDistributedEventHandler<T>
        {
            if (configurator == null) throw new ArgumentNullException(nameof(configurator));
            if (registrationContext == null) throw new ArgumentNullException(nameof(registrationContext));

            configurator.RegisterDistributedEventHandlers<T, TH1>(registrationContext);
            configurator.RegisterDistributedEventHandlers<T, TH2>(registrationContext);
        }

        /// <summary>
        /// Register template consumer
        /// </summary>
        /// <param name="configurator">IBusRegistrationConfigurator</param>
        /// <param name="registrationContext">IBusRegistrationContext</param>
        /// <typeparam name="T">DistributedBaseEventParams</typeparam>
        /// <typeparam name="TH1"></typeparam>
        /// <typeparam name="TH2"></typeparam>
        /// <typeparam name="TH3"></typeparam>
        public static void RegisterDistributedEventHandlers<T, TH1, TH2, TH3>(this IReceiveConfigurator<IReceiveEndpointConfigurator> configurator,
            IBusRegistrationContext registrationContext)
            where T : DistributedBaseEventParams
            where TH1 : IDistributedEventHandler<T>
            where TH2 : IDistributedEventHandler<T>
            where TH3 : IDistributedEventHandler<T>
        {
            if (configurator == null) throw new ArgumentNullException(nameof(configurator));
            if (registrationContext == null) throw new ArgumentNullException(nameof(registrationContext));

            configurator.RegisterDistributedEventHandlers<T, TH1>(registrationContext);
            configurator.RegisterDistributedEventHandlers<T, TH2>(registrationContext);
            configurator.RegisterDistributedEventHandlers<T, TH3>(registrationContext);
        }
    }
}