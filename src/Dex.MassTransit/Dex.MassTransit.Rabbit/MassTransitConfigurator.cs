using System;
using System.Collections.Generic;
using System.Linq;
using Dex.Extensions;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using MassTransit.RabbitMqTransport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Dex.MassTransit.Rabbit
{
    public static class MassTransitConfigurator
    {
        /// <summary>
        /// Register bus
        /// </summary>
        /// <param name="collectionConfigurator"></param>
        /// <param name="registerConsumers"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterBus(this IServiceCollectionBusConfigurator collectionConfigurator,
            Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator> registerConsumers)
        {
            if (collectionConfigurator == null) throw new ArgumentNullException(nameof(collectionConfigurator));
            if (registerConsumers == null) throw new ArgumentNullException(nameof(registerConsumers));

            collectionConfigurator.UsingRabbitMq((registrationContext, mqBusFactoryConfigurator) =>
            {
                var rabbitMqOptions = registrationContext.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                mqBusFactoryConfigurator.Host(rabbitMqOptions.Host, rabbitMqOptions.Port, rabbitMqOptions.VHost,
                    $"{Environment.MachineName}:{AppDomain.CurrentDomain.FriendlyName}");

                mqBusFactoryConfigurator.AutoDelete = false;
                mqBusFactoryConfigurator.Durable = true;

                // mqBusFactoryConfigurator.ConnectConsumeObserver(busRegistrationContext.GetRequiredService<LogIdMassTransitObserver>()); // TODO

                // register consumers here
                registerConsumers(registrationContext, mqBusFactoryConfigurator);
            });
        }

        /// <summary>
        /// Register receive EndPoint and bind with type T
        /// </summary>
        /// <param name="busRegistrationContext">bus registration contex</param>
        /// <param name="busFactoryConfigurator">configuration factory</param>
        /// <param name="endpointConsumerConfigurator">configure consumer delegate</param>
        /// <param name="isForPublish">true for Publish, false when only Send</param>
        /// <typeparam name="TD">Consumer message type</typeparam>
        /// <typeparam name="T">Consumer type</typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterReceiveEndpoint<T, TD>(this IBusRegistrationContext busRegistrationContext, IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            Action<IEndpointConfigurator> endpointConsumerConfigurator = null, bool isForPublish = false)
            where T : class, IConsumer<TD>
            where TD : class, IConsumer, new()
        {
            if (busRegistrationContext == null) throw new ArgumentNullException(nameof(busRegistrationContext));
            if (busFactoryConfigurator == null) throw new ArgumentNullException(nameof(busFactoryConfigurator));

            RegisterConsumersEndpoint<TD>(busRegistrationContext, busFactoryConfigurator, endpointConsumerConfigurator, new[] {typeof(T)}, isForPublish);
        }
        
        /// <summary>
        /// Register and bind SendEndpoint for type T
        /// </summary>
        /// <param name="provider"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static UriBuilder RegisterSendEndPoint<T>(this IServiceProvider provider) where T : class, IConsumer, new()
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            var endPoint = CreateQueueNameFromType<T>(provider);
            if (!EndpointConvention.TryGetDestinationAddress<T>(out _))
            {
                EndpointConvention.Map<T>(endPoint.Uri);
            }

            return endPoint;
        }

        // private  

        private static UriBuilder CreateQueueNameFromType<T>(this IServiceProvider provider) where T : class, IConsumer, new()
        {
            var queueName = typeof(T).Name.ToLower().ReplaceRegex("dto$", "");
            var mqOptions = provider.GetRequiredService<IOptions<RabbitMqOptions>>();
            return new UriBuilder("rabbitmq", mqOptions.Value.Host, mqOptions.Value.Port, queueName);
        }

        private static void RegisterConsumersEndpoint<TD>(IRegistration busRegistrationContext, IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            Action<IRabbitMqReceiveEndpointConfigurator> endpointConsumerConfigurator, IEnumerable<Type> types, bool isForPublish = false) where TD : class, IConsumer, new()
        {
            var endPoint = isForPublish ? busRegistrationContext.CreateQueueNameFromType<TD>() : busRegistrationContext.RegisterSendEndPoint<TD>();

            var last = endPoint.ToString()
                .TrimEnd('/')
                .Split('/')
                .Last();

            foreach (var consumerType in types)
            {
                var queueName = isForPublish
                    ? last + "_" + consumerType.Name
                    : last;

                busFactoryConfigurator.ReceiveEndpoint(queueName, configurator =>
                {
                    if (endpointConsumerConfigurator == null)
                    {
                        configurator.UseMessageRetry(x =>
                        {
                            // default policy
                            x.SetRetryPolicy(filter => filter.Interval(10, 1.Minutes()));
                        });
                    }
                    else
                    {
                        endpointConsumerConfigurator.Invoke(configurator);
                    }

                    configurator.ConfigureConsumer(busRegistrationContext, consumerType);
                });
            }
        }
    }
}