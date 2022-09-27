using System;
using System.Reflection;
using Dex.MassTransit.Rabbit;
using MassTransit;

namespace DistributedEvents
{
    public static class MassTransitExtensions
    {
        /// <summary>
        /// Register template consumer
        /// </summary>
        /// <param name="configurator">IBusRegistrationConfigurator</param>
        /// <typeparam name="T">DistributedBaseEventParams</typeparam>
        public static void RegisterDistributedEventConsumer<T>(this IBusRegistrationConfigurator configurator)
            where T : DistributedBaseEventParams
        {
            if (configurator == null) throw new ArgumentNullException(nameof(configurator));

            configurator.AddConsumer<DistributedEventConsumer<T>>();
        }

        /// <summary>
        /// Register SendEndPoint
        /// </summary>
        /// <param name="context">IBusRegistrationContext</param>
        /// <param name="options">Overridden RabbitMqOptions</param>
        /// <typeparam name="T">DistributedBaseEventParams</typeparam>
        public static void RegisterDistributedEventSendEndPoint<T>(this IBusRegistrationContext context, RabbitMqOptions? options = null)
            where T : DistributedBaseEventParams
        {
            context.RegisterSendEndPoint<T>(options);
        }

        /// <summary>
        /// Register ReceiveEndpoint
        /// </summary>
        /// <param name="context">IBusRegistrationContext</param>
        /// <param name="configurator">IRabbitMqBusFactoryConfigurator</param>
        /// <param name="serviceName">Service name in which the method was called</param>
        /// <param name="options">Overridden RabbitMqOptions</param>
        /// <typeparam name="T">DistributedBaseEventParams</typeparam>
        public static void RegisterDistributedEventReceiveEndpoint<T>(this IBusRegistrationContext context, IRabbitMqBusFactoryConfigurator configurator,
            string? serviceName = null, RabbitMqOptions? options = null)
            where T : DistributedBaseEventParams
        {
            serviceName = string.IsNullOrWhiteSpace(serviceName)
                ? Assembly.GetCallingAssembly().GetName().Name
                : serviceName;

            context.RegisterReceiveEndpoint<DistributedEventConsumer<T>, T>(configurator, createSeparateQueue: true, serviceName: serviceName,
                rabbitMqOptions: options);
        }
    }
}