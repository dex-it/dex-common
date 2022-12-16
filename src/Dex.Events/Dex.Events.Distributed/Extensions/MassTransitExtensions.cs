using System;
using System.Reflection;
using Dex.Events.Distributed.Models;
using Dex.Extensions;
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
        /// <param name="serviceName">Prefix for queue name</param>
        /// <typeparam name="TEventParams">DistributedBaseEventParams</typeparam>
        /// <typeparam name="TDistributedEventHandler">Type of event handler</typeparam>
        public static void SubscribeEventHandlers<TEventParams, TDistributedEventHandler>(this IReceiveConfigurator<IReceiveEndpointConfigurator> configurator,
            IBusRegistrationContext registrationContext, string? serviceName = null)
            where TEventParams : DistributedBaseEventParams
            where TDistributedEventHandler : IDistributedEventHandler<TEventParams>
        {
            if (configurator == null) throw new ArgumentNullException(nameof(configurator));
            if (registrationContext == null) throw new ArgumentNullException(nameof(registrationContext));

            serviceName = serviceName ?? Assembly.GetCallingAssembly().GetName().Name;

            var queueName = typeof(TEventParams).Name.ReplaceRegex("(?i)dto(?-i)$", "");
            var consumerName = typeof(TDistributedEventHandler).Name.Replace("`", string.Empty, StringComparison.OrdinalIgnoreCase);
            var fullName = $"Event_{serviceName}_{queueName}_{consumerName}";

            configurator.ReceiveEndpoint(fullName, x => x.ConfigureConsumer(registrationContext, typeof(TDistributedEventHandler)));
        }

        /// <summary>
        /// Register template consumer
        /// </summary>
        /// <param name="configurator">IBusRegistrationConfigurator</param>
        /// <param name="registrationContext">IBusRegistrationContext</param>
        /// <param name="serviceName">Prefix for queue name</param>
        /// <typeparam name="TEventParams">DistributedBaseEventParams</typeparam>
        /// <typeparam name="TDistributedEventHandler1">Type of event handler</typeparam>
        /// <typeparam name="TDistributedEventHandler2">Type of event handler</typeparam>
        public static void SubscribeEventHandlers<TEventParams, TDistributedEventHandler1, TDistributedEventHandler2>(
            this IReceiveConfigurator<IReceiveEndpointConfigurator> configurator,
            IBusRegistrationContext registrationContext, string? serviceName = null)
            where TEventParams : DistributedBaseEventParams
            where TDistributedEventHandler1 : IDistributedEventHandler<TEventParams>
            where TDistributedEventHandler2 : IDistributedEventHandler<TEventParams>
        {
            if (configurator == null) throw new ArgumentNullException(nameof(configurator));
            if (registrationContext == null) throw new ArgumentNullException(nameof(registrationContext));

            configurator.SubscribeEventHandlers<TEventParams, TDistributedEventHandler1>(registrationContext, serviceName);
            configurator.SubscribeEventHandlers<TEventParams, TDistributedEventHandler2>(registrationContext, serviceName);
        }

        /// <summary>
        /// Register template consumer
        /// </summary>
        /// <param name="configurator">IBusRegistrationConfigurator</param>
        /// <param name="registrationContext">IBusRegistrationContext</param>
        /// <param name="serviceName">Prefix for queue name</param>
        /// <typeparam name="TEventParams">DistributedBaseEventParams</typeparam>
        /// <typeparam name="TDistributedEventHandler1">Type of event handler</typeparam>
        /// <typeparam name="TDistributedEventHandler2">Type of event handler</typeparam>
        /// <typeparam name="TDistributedEventHandler3">Type of event handler</typeparam>
        public static void SubscribeEventHandlers<TEventParams, TDistributedEventHandler1, TDistributedEventHandler2, TDistributedEventHandler3>(
            this IReceiveConfigurator<IReceiveEndpointConfigurator> configurator, IBusRegistrationContext registrationContext, string? serviceName = null)
            where TEventParams : DistributedBaseEventParams
            where TDistributedEventHandler1 : IDistributedEventHandler<TEventParams>
            where TDistributedEventHandler2 : IDistributedEventHandler<TEventParams>
            where TDistributedEventHandler3 : IDistributedEventHandler<TEventParams>
        {
            if (configurator == null) throw new ArgumentNullException(nameof(configurator));
            if (registrationContext == null) throw new ArgumentNullException(nameof(registrationContext));

            configurator.SubscribeEventHandlers<TEventParams, TDistributedEventHandler1>(registrationContext, serviceName);
            configurator.SubscribeEventHandlers<TEventParams, TDistributedEventHandler2>(registrationContext, serviceName);
            configurator.SubscribeEventHandlers<TEventParams, TDistributedEventHandler3>(registrationContext, serviceName);
        }
    }
}