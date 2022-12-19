using System;
using System.Collections.Generic;
using Dex.Extensions;
using Dex.MassTransit.ActivityTrace;
using MassTransit;
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
        /// <param name="collectionConfigurator"></param>
        /// <param name="registerConsumers">Register consumers action</param>
        /// <param name="rabbitMqOptions">By default options RabbitMqOptions, will be injected from DI</param>
        /// <param name="refreshConnectCallback">Call before reconnect action will be performed</param>
        /// <exception cref="ArgumentNullException"/>
        public static void RegisterBus(this IBusRegistrationConfigurator collectionConfigurator,
            Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator> registerConsumers, RabbitMqOptions? rabbitMqOptions = null,
            Func<IBusRegistrationContext, RefreshConnectionFactoryCallback>? refreshConnectCallback = null)
        {
            RegisterBus<RabbitMqOptions>(collectionConfigurator, registerConsumers, rabbitMqOptions, refreshConnectCallback);
        }

        /// <summary>
        /// Register bus.
        /// </summary>
        /// <param name="collectionConfigurator"></param>
        /// <param name="registerConsumers">Register consumers action</param>
        /// <param name="rabbitMqOptions">By default options RabbitMqOptions, will be injected from DI</param>
        /// <param name="refreshConnectCallback">Call before reconnect action will be performed</param>
        /// <typeparam name="TMqOptions">Configuration options type</typeparam>
        /// <exception cref="ArgumentNullException"/>
        public static void RegisterBus<TMqOptions>(this IBusRegistrationConfigurator collectionConfigurator,
            Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator> registerConsumers, TMqOptions? rabbitMqOptions = null,
            Func<IBusRegistrationContext, RefreshConnectionFactoryCallback>? refreshConnectCallback = null) where TMqOptions : RabbitMqOptions, new()
        {
            if (collectionConfigurator == null) throw new ArgumentNullException(nameof(collectionConfigurator));
            if (registerConsumers == null) throw new ArgumentNullException(nameof(registerConsumers));

            collectionConfigurator.UsingRabbitMq((registrationContext, mqBusFactoryConfigurator) =>
            {
                rabbitMqOptions ??= registrationContext.GetRequiredService<IOptions<TMqOptions>>().Value;
                mqBusFactoryConfigurator.Host(rabbitMqOptions.Host, rabbitMqOptions.Port, rabbitMqOptions.VHost,
                    $"{Environment.MachineName}:{AppDomain.CurrentDomain.FriendlyName}", configurator =>
                    {
                        configurator.OnRefreshConnectionFactory = refreshConnectCallback?.Invoke(registrationContext);
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
        /// <remarks>By default options RabbitMqOptions, will be injected from DI</remarks>
        /// <param name="busRegistrationContext">Bus registration contex.</param>
        /// <param name="busFactoryConfigurator">Configuration factory.</param>
        /// <param name="endpointConsumerConfigurator">Configure consumer delegate.</param>
        /// <param name="rabbitMqOptions">Connection parameters. Affected only mapping SendEndpoint, if empty, then injected from DI.</param>
        /// <param name="createSeparateQueue">Create separate Queue for consumer. It is allow to process same type messages with different consumers. It is pub-sub pattern.</param>
        /// <param name="serviceName">Extra service name, queue prefix</param>
        /// <typeparam name="T">Consumer type.</typeparam>
        /// <typeparam name="TMessage">Consumer message type.</typeparam>
        /// <exception cref="ArgumentNullException"/>
        public static void RegisterReceiveEndpoint<T, TMessage>(this IBusRegistrationContext busRegistrationContext,
            IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            Action<IEndpointConfigurator>? endpointConsumerConfigurator = null, RabbitMqOptions? rabbitMqOptions = null,
            bool createSeparateQueue = false, string? serviceName = null)
            where T : class, IConsumer<TMessage>
            where TMessage : class
        {
            RegisterReceiveEndpoint<T, TMessage, RabbitMqOptions>(busRegistrationContext, busFactoryConfigurator, endpointConsumerConfigurator,
                rabbitMqOptions, createSeparateQueue, serviceName);
        }

        /// <summary>
        /// Register receive EndPoint and bind with type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>If createSeparateQueue = false, then call RegisterSendEndPoint, to allow sending messages TMessage by call Send</remarks>
        /// <param name="busRegistrationContext">Bus registration contex.</param>
        /// <param name="busFactoryConfigurator">Configuration factory.</param>
        /// <param name="endpointConsumerConfigurator">Configure consumer delegate.</param>
        /// <param name="rabbitMqOptions">By default options RabbitMqOptions, will be injected from DI</param>
        /// <param name="createSeparateQueue">Create separate Queue for consumer. It is allow to process same type messages with different consumers. It is pub-sub pattern.</param>
        /// <param name="serviceName">Extra service name, queue prefix</param>
        /// <typeparam name="T">Consumer type.</typeparam>
        /// <typeparam name="TMessage">Consumer message type.</typeparam>
        /// <typeparam name="TMqOptions">Options type. Affetced only mapping SendEndpoint</typeparam>
        /// <exception cref="ArgumentNullException"/>
        public static void RegisterReceiveEndpoint<T, TMessage, TMqOptions>(this IBusRegistrationContext busRegistrationContext,
            IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            Action<IEndpointConfigurator>? endpointConsumerConfigurator = null, TMqOptions? rabbitMqOptions = null,
            bool createSeparateQueue = false, string? serviceName = null)
            where T : class, IConsumer<TMessage>
            where TMessage : class
            where TMqOptions : RabbitMqOptions, new()
        {
            if (busRegistrationContext == null) throw new ArgumentNullException(nameof(busRegistrationContext));
            if (busFactoryConfigurator == null) throw new ArgumentNullException(nameof(busFactoryConfigurator));

            RegisterConsumersEndpoint<TMessage, TMqOptions>(busRegistrationContext, busFactoryConfigurator, endpointConsumerConfigurator,
                new[] { typeof(T) }, rabbitMqOptions, createSeparateQueue, serviceName);
        }

        /// <summary>
        /// Register and bind SendEndpoint for type <typeparamref name="TMessage"/>.
        /// <remarks>You need correct to fill options, mapping will be performed between message type and full endpoint address</remarks>
        /// <param name="rabbitMqOptions">By default options RabbitMqOptions, will be injected from DI</param>
        /// <param name="provider">IServiceProvider</param>
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public static UriBuilder RegisterSendEndPoint<TMessage>(this IServiceProvider provider, RabbitMqOptions? rabbitMqOptions = null)
            where TMessage : class
        {
            return RegisterSendEndPoint<TMessage, RabbitMqOptions>(provider, rabbitMqOptions);
        }

        /// <summary>
        /// Register and bind SendEndpoint for type <typeparamref name="TMessage"/>.
        /// </summary>
        /// <remarks>You need correct to fill options, mapping will be performed between message type and full endpoint address</remarks>
        /// <param name="provider">IServiceProvider</param>
        /// <param name="rabbitMqOptions">By default options RabbitMqOptions, will be injected from DI</param>
        /// <typeparam name="TMessage">Consumer message type.</typeparam>
        /// <typeparam name="TMqOptions">Options type. Affetced only mapping SendEndpoint</typeparam>
        /// <exception cref="ArgumentNullException"/>
        public static UriBuilder RegisterSendEndPoint<TMessage, TMqOptions>(this IServiceProvider provider, TMqOptions? rabbitMqOptions = null)
            where TMessage : class
            where TMqOptions : RabbitMqOptions, new()
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            var endPoint = CreateQueueNameFromType<TMessage, TMqOptions>(provider, rabbitMqOptions);
            if (!EndpointConvention.TryGetDestinationAddress<TMessage>(out _))
            {
                EndpointConvention.Map<TMessage>(endPoint.Uri);
            }

            return endPoint;
        }

        // private  

        private static UriBuilder CreateQueueNameFromType<TMessage, TMqOptions>(this IServiceProvider provider, TMqOptions? rabbitMqOptions)
            where TMessage : class where TMqOptions : RabbitMqOptions, new()
        {
            var queueName = QueueNameConventionHelper.GetOnlyQueueName<TMessage>();
            var mqOptions = rabbitMqOptions ?? provider.GetRequiredService<IOptions<TMqOptions>>().Value;
            return new UriBuilder(mqOptions + "/" + queueName);
        }

        private static void RegisterConsumersEndpoint<TMessage, TMqOptions>(IRegistrationContext busRegistrationContext,
            IRabbitMqBusFactoryConfigurator busFactoryConfigurator,
            Action<IRabbitMqReceiveEndpointConfigurator>? endpointConsumerConfigurator, IEnumerable<Type> types, TMqOptions? rabbitMqOptions,
            bool createSeparateQueue = false, string? serviceName = null)
            where TMessage : class where TMqOptions : RabbitMqOptions, new()
        {
            var endPoint = createSeparateQueue
                ? busRegistrationContext.CreateQueueNameFromType<TMessage, TMqOptions>(rabbitMqOptions)
                : busRegistrationContext.RegisterSendEndPoint<TMessage, TMqOptions>(rabbitMqOptions);

            foreach (var consumerType in types)
            {
                var queueName = createSeparateQueue
                    ? endPoint.Uri.GetName(consumerType, serviceName)
                    : endPoint.Uri.GetOnlyQueueName();

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