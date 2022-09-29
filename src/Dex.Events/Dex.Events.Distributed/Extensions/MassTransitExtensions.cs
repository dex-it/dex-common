using System;
using System.Reflection;
using Dex.Events.Distributed.Models;
using Dex.MassTransit.Rabbit;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Events.Distributed.Extensions
{
    public static class MassTransitExtensions
    {
        /// <summary>
        /// Register template consumer
        /// </summary>
        /// <param name="configurator">IBusRegistrationConfigurator</param>
        /// <typeparam name="T">DistributedBaseEventParams</typeparam>
        /// <typeparam name="TH1"></typeparam>
        public static void RegisterDistributedEventHandlers<T, TH1>(this IBusRegistrationConfigurator configurator)
            where T : DistributedBaseEventParams
            where TH1 : IDistributedEventHandler<T>
        {
            configurator.RegisterDistributedEventHandlers<T>(typeof(TH1));
        }

        /// <summary>
        /// Register template consumer
        /// </summary>
        /// <param name="configurator">IBusRegistrationConfigurator</param>
        /// <typeparam name="T">DistributedBaseEventParams</typeparam>
        /// <typeparam name="TH1"></typeparam>
        /// <typeparam name="TH2"></typeparam>
        public static void RegisterDistributedEventHandlers<T, TH1, TH2>(this IBusRegistrationConfigurator configurator)
            where T : DistributedBaseEventParams
            where TH1 : IDistributedEventHandler<T>
            where TH2 : IDistributedEventHandler<T>
        {
            configurator.RegisterDistributedEventHandlers<T>(typeof(TH1), typeof(TH2));
        }

        /// <summary>
        /// Register template consumer
        /// </summary>
        /// <param name="configurator">IBusRegistrationConfigurator</param>
        /// <typeparam name="T">DistributedBaseEventParams</typeparam>
        /// <typeparam name="TH1"></typeparam>
        /// <typeparam name="TH2"></typeparam>
        /// <typeparam name="TH3"></typeparam>
        public static void RegisterDistributedEventHandlers<T, TH1, TH2, TH3>(this IBusRegistrationConfigurator configurator)
            where T : DistributedBaseEventParams
            where TH1 : IDistributedEventHandler<T>
            where TH2 : IDistributedEventHandler<T>
            where TH3 : IDistributedEventHandler<T>
        {
            configurator.RegisterDistributedEventHandlers<T>(typeof(TH1), typeof(TH2), typeof(TH3));
        }

        /// <summary>
        /// Register template consumer
        /// </summary>
        /// <param name="configurator">IBusRegistrationConfigurator</param>
        /// <param name="handlersTypes">Handlers types</param>
        /// <typeparam name="T">DistributedBaseEventParams</typeparam>
        private static void RegisterDistributedEventHandlers<T>(this IBusRegistrationConfigurator configurator, params Type[] handlersTypes)
            where T : DistributedBaseEventParams
        {
            if (configurator == null) throw new ArgumentNullException(nameof(configurator));
            if (handlersTypes == null) throw new ArgumentNullException(nameof(handlersTypes));

            configurator.AddConsumer<DistributedEventConsumer<T>>();
            foreach (var handlerType in handlersTypes)
            {
                configurator.AddScoped(typeof(IDistributedEventHandler<T>), handlerType);
            }
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