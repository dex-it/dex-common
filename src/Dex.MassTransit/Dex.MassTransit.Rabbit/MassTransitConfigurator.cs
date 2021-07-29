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
        /// Register bus.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public static void RegisterBus(this IServiceCollectionBusConfigurator collectionConfigurator,
            Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator> registerConsumers)
        {
            if (collectionConfigurator == null) throw new ArgumentNullException(nameof(collectionConfigurator));
            if (registerConsumers == null) throw new ArgumentNullException(nameof(registerConsumers));

            collectionConfigurator.UsingRabbitMq((registrationContext, mqBusFactoryConfigurator) =>
            {
                var rabbitMqOptions = registrationContext.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                mqBusFactoryConfigurator.Host(rabbitMqOptions.Host, rabbitMqOptions.Port, rabbitMqOptions.VHost,
                    $"{Environment.MachineName}:{AppDomain.CurrentDomain.FriendlyName}", configurator =>
                    {
                        configurator.Username(rabbitMqOptions.Username);
                        configurator.Password(rabbitMqOptions.Password);
                        if (rabbitMqOptions.IsSecure && rabbitMqOptions.CertificatePath != null)
                        {
                            configurator.UseSsl(ssl => ssl.CertificatePath = rabbitMqOptions.CertificatePath);
                        }
                    });

                mqBusFactoryConfigurator.AutoDelete = false;
                mqBusFactoryConfigurator.Durable = true;

                // mqBusFactoryConfigurator.ConnectConsumeObserver(busRegistrationContext.GetRequiredService<LogIdMassTransitObserver>()); // TODO

                // register consumers here
                registerConsumers(registrationContext, mqBusFactoryConfigurator);
            });
        }

        /// <summary>
        /// Register receive EndPoint and bind with type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="busRegistrationContext">Bus registration contex.</param>
        /// <param name="busFactoryConfigurator">Configuration factory.</param>
        /// <param name="endpointConsumerConfigurator">Configure consumer delegate.</param>
        /// <param name="isForPublish">True for Publish, false when only Send.</param>
        /// <typeparam name="T">Consumer type.</typeparam>
        /// <typeparam name="TMessage">Consumer message type.</typeparam>
        /// <exception cref="ArgumentNullException"/>
        public static void RegisterReceiveEndpoint<T, TMessage>(this IBusRegistrationContext busRegistrationContext, IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            Action<IEndpointConfigurator>? endpointConsumerConfigurator = null, bool isForPublish = false)
            where T : class, IConsumer<TMessage>
            where TMessage : class
        {
            if (busRegistrationContext == null) throw new ArgumentNullException(nameof(busRegistrationContext));
            if (busFactoryConfigurator == null) throw new ArgumentNullException(nameof(busFactoryConfigurator));

            RegisterConsumersEndpoint<TMessage>(busRegistrationContext, busFactoryConfigurator, endpointConsumerConfigurator, new[] {typeof(T)}, isForPublish);
        }

        /// <summary>
        /// Register and bind SendEndpoint for type <typeparamref name="TMessage"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public static UriBuilder RegisterSendEndPoint<TMessage>(this IServiceProvider provider)
            where TMessage : class
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            var endPoint = CreateQueueNameFromType<TMessage>(provider);

            if (!EndpointConvention.TryGetDestinationAddress<TMessage>(out _))
            {
                EndpointConvention.Map<TMessage>(endPoint.Uri);
            }

            return endPoint;
        }

        // private  

        private static UriBuilder CreateQueueNameFromType<T>(this IServiceProvider provider)
            where T : class
        {
            var queueName = typeof(T).Name.ToLowerInvariant().ReplaceRegex("dto$", "");
            var mqOptions = provider.GetRequiredService<IOptions<RabbitMqOptions>>();
            return new UriBuilder("rabbitmq", mqOptions.Value.Host, mqOptions.Value.Port, queueName);
        }

        private static void RegisterConsumersEndpoint<TMessage>(IRegistration busRegistrationContext, IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            Action<IRabbitMqReceiveEndpointConfigurator>? endpointConsumerConfigurator, IEnumerable<Type> types, bool isForPublish = false)
            where TMessage : class
        {
            var endPoint = isForPublish ? busRegistrationContext.CreateQueueNameFromType<TMessage>() : busRegistrationContext.RegisterSendEndPoint<TMessage>();

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