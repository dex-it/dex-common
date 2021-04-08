using System;
using System.Collections.Generic;
using System.Linq;
using Dex.Extensions;
using Dex.MassTransit.Extensions;
using Dex.MassTransit.Extensions.Options;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Dex.MassTransit.Rabbit
{
    public static class MassTransitConfigurator
    {
        public static void RegisterBus(this IServiceCollectionBusConfigurator collectionConfigurator, Action<IBusRegistrationContext, IBusFactoryConfigurator> registerConsumers)
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
        /// Register recieve EndPoint and bind with type T
        /// </summary>
        /// <param name="provider">bus registration contex</param>
        /// <param name="cfg">configuration factory</param>
        /// <param name="endpointConsumer">configure consumer delegate</param>
        /// <param name="isForPublish">true for Publish, false when only Send</param>
        /// <typeparam name="TD">Consumer message type</typeparam>
        /// <typeparam name="T">Consumer type</typeparam>
        /// <typeparam name="T1">Consumer type</typeparam>
        /// <typeparam name="T2">Consumer type</typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterReceiveEndpoint<T, T1, T2, TD>(this IBusRegistrationContext provider, IBusFactoryConfigurator cfg,
            Action<IEndpointConfigurator> endpointConsumer = null, bool isForPublish = true)
            where T : class, IConsumer<TD>
            where TD : class, IConsumer, new()
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));

            var endPoint = isForPublish ? provider.CreateQueueNameFromType<TD>() : provider.RegisterSendEndPoint<TD>();
            RegisterConsumersEndpoint(provider, cfg, endPoint, endpointConsumer, new[] {typeof(T), typeof(T1), typeof(T2)}, isForPublish);
        }

        /// <summary>
        /// Register recieve EndPoint and bind with type T
        /// </summary>
        /// <param name="provider">bus registration contex</param>
        /// <param name="cfg">configuration factory</param>
        /// <param name="endpointConsumer">configure consumer delegate</param>
        /// <param name="isForPublish">true for Publish, false when only Send</param>
        /// <typeparam name="TD">Consumer message type</typeparam>
        /// <typeparam name="T">Consumer type</typeparam>
        /// <typeparam name="T1">Consumer type</typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterReceiveEndpoint<T, T1, TD>(this IBusRegistrationContext provider, IBusFactoryConfigurator cfg,
            Action<IEndpointConfigurator> endpointConsumer = null, bool isForPublish = true)
            where T : class, IConsumer<TD>
            where TD : class, IConsumer, new()
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));

            var endPoint = isForPublish ? provider.CreateQueueNameFromType<TD>() : provider.RegisterSendEndPoint<TD>();
            RegisterConsumersEndpoint(provider, cfg, endPoint, endpointConsumer, new[] {typeof(T), typeof(T1)}, isForPublish);
        }

        /// <summary>
        /// Register recieve EndPoint and bind with type T
        /// </summary>
        /// <param name="provider">bus registration contex</param>
        /// <param name="cfg">configuration factory</param>
        /// <param name="endpointConsumer">configure consumer delegate</param>
        /// <param name="isForPublish">true for Publish, false when only Send</param>
        /// <typeparam name="TD">Consumer message type</typeparam>
        /// <typeparam name="T">Consumer type</typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterReceiveEndpoint<T, TD>(this IBusRegistrationContext provider, IBusFactoryConfigurator cfg,
            Action<IEndpointConfigurator> endpointConsumer = null, bool isForPublish = false)
            where T : class, IConsumer<TD>
            where TD : class, IConsumer, new()
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));

            var endPoint = isForPublish ? provider.CreateQueueNameFromType<TD>() : provider.RegisterSendEndPoint<TD>();
            RegisterConsumersEndpoint(provider, cfg, endPoint, endpointConsumer, new[] {typeof(T)}, isForPublish);
        }

        /// <summary>
        /// Configure recieve endpoint for Fifo queues
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="cfg"></param>
        /// <param name="endpointConsumer"></param>
        /// <param name="isForPublish"></param>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TD"></typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ConfigurationException"></exception>
        public static void RegisterReceiveEndpointAsFifo<T, TD>(this IBusRegistrationContext provider, IBusFactoryConfigurator cfg,
            Action<IEndpointConfigurator> endpointConsumer = null, bool isForPublish = false)
            where T : class, IConsumer<TD>
            where TD : class, IConsumer, new()
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));

            var endPoint = isForPublish ? provider.CreateQueueNameFromType<TD>() : provider.RegisterSendEndPoint<TD>();
            RegisterConsumersEndpoint(provider, cfg, endPoint, configurator => { endpointConsumer?.Invoke(configurator); }, new[] {typeof(T)}, isForPublish);
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

        private static void RegisterConsumersEndpoint(IRegistration busRegistrationContext, IReceiveConfigurator cfg, UriBuilder endPoint,
            Action<IEndpointConfigurator> endpointConsumerConfigurator, IEnumerable<Type> types, bool isForPublish = false)
        {
            var last = endPoint.ToString()
                .TrimEnd('/')
                .Split('/')
                .Last();

            foreach (var consumerType in types)
            {
                var queueName = isForPublish
                    ? last + "_" + consumerType.Name
                    : last;

                cfg.ReceiveEndpoint(queueName, configurator =>
                {
                    if (endpointConsumerConfigurator == null)
                    {
                        configurator.UseMessageRetry(x =>
                        {
                            // default policy
                            x.SetRetryPolicy(filter => filter.Interval(256, 1.Minutes()));
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