using System;
using System.Collections.Generic;
using System.Linq;
using Dex.Extensions;
using Dex.MassTransit.ActivityTrace;
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
        /// Enable Activity.TraceId propagation for all Consumers
        /// </summary>
        public static bool EnableConsumerTracer { get; set; } = true;


        /// <summary>
        /// Register bus.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public static void RegisterBus(this IServiceCollectionBusConfigurator collectionConfigurator,
            Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator> registerConsumers, RabbitMqOptions? rabbitMqOptions = null)
        {
            if (collectionConfigurator == null) throw new ArgumentNullException(nameof(collectionConfigurator));
            if (registerConsumers == null) throw new ArgumentNullException(nameof(registerConsumers));

            collectionConfigurator.UsingRabbitMq((registrationContext, mqBusFactoryConfigurator) =>
            {
                rabbitMqOptions ??= registrationContext.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
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

                // enable activity tracer for all consumers
                if (EnableConsumerTracer)
                    mqBusFactoryConfigurator.LinkActivityTracingContext();

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
        /// <param name="rabbitMqOptions">Force connection params.</param>
        /// <param name="createSeparateQueue">Create separate Queue for consumer. It is allow to process same type messages with different consumers. It is publish-consumer pattern.</param>
        /// <typeparam name="T">Consumer type.</typeparam>
        /// <typeparam name="TMessage">Consumer message type.</typeparam>
        /// <exception cref="ArgumentNullException"/>
        public static void RegisterReceiveEndpoint<T, TMessage>(this IBusRegistrationContext busRegistrationContext, IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            Action<IEndpointConfigurator>? endpointConsumerConfigurator = null, RabbitMqOptions? rabbitMqOptions = null, bool createSeparateQueue = false)
            where T : class, IConsumer<TMessage>
            where TMessage : class
        {
            if (busRegistrationContext == null) throw new ArgumentNullException(nameof(busRegistrationContext));
            if (busFactoryConfigurator == null) throw new ArgumentNullException(nameof(busFactoryConfigurator));

            RegisterConsumersEndpoint<TMessage>(busRegistrationContext, busFactoryConfigurator, endpointConsumerConfigurator, new[] { typeof(T) }, rabbitMqOptions, createSeparateQueue);
        }

        /// <summary>
        /// Register and bind SendEndpoint for type <typeparamref name="TMessage"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public static UriBuilder RegisterSendEndPoint<TMessage>(this IServiceProvider provider, RabbitMqOptions? rabbitMqOptions = null)
            where TMessage : class
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            var endPoint = CreateQueueNameFromType<TMessage>(provider, rabbitMqOptions);
            if (!EndpointConvention.TryGetDestinationAddress<TMessage>(out _))
            {
                EndpointConvention.Map<TMessage>(endPoint.Uri);
            }

            return endPoint;
        }

        // private  

        private static UriBuilder CreateQueueNameFromType<TMessage>(this IServiceProvider provider, RabbitMqOptions? rabbitMqOptions)
            where TMessage : class
        {
            var queueName = typeof(TMessage).Name.ReplaceRegex("(?i)dto(?-i)$", "");
            var mqOptions = rabbitMqOptions ?? provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            return new UriBuilder(mqOptions + "/" + queueName);
        }

        private static void RegisterConsumersEndpoint<TMessage>(IRegistration busRegistrationContext, IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            Action<IRabbitMqReceiveEndpointConfigurator>? endpointConsumerConfigurator, IEnumerable<Type> types, RabbitMqOptions? rabbitMqOptions, bool createSeparateQueue = false)
            where TMessage : class
        {
            var endPoint = createSeparateQueue
                ? busRegistrationContext.CreateQueueNameFromType<TMessage>(rabbitMqOptions)
                : busRegistrationContext.RegisterSendEndPoint<TMessage>(rabbitMqOptions);

            var qName = endPoint.Uri.Segments.Last();

            foreach (var consumerType in types)
            {
                var queueName = createSeparateQueue
                    ? qName + "_" + consumerType.Name
                    : qName;

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