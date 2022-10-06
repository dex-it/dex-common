using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.SQS;
using Dex.Extensions;
using Dex.MassTransit.ActivityTrace;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Dex.MassTransit.SQS
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
        public static void RegisterBus(this IBusRegistrationConfigurator collectionConfigurator,
            Action<IBusRegistrationContext, IAmazonSqsBusFactoryConfigurator> registerConsumers, AmazonMqOptions? amazonMqOptions = null)
        {
            if (collectionConfigurator == null) throw new ArgumentNullException(nameof(collectionConfigurator));
            if (registerConsumers == null) throw new ArgumentNullException(nameof(registerConsumers));

            collectionConfigurator.UsingAmazonSqs((busRegistrationContext, mqBusFactoryConfigurator) =>
            {
                amazonMqOptions ??= busRegistrationContext.GetRequiredService<IOptions<AmazonMqOptions>>().Value;
                mqBusFactoryConfigurator.Host(amazonMqOptions.Region, configurator =>
                {
                    configurator.AccessKey(amazonMqOptions.AccessKey);
                    configurator.SecretKey(amazonMqOptions.SecretKey);
                });

                mqBusFactoryConfigurator.AutoDelete = false;
                mqBusFactoryConfigurator.Durable = true;

                // enable activity tracer for all consumers
                if (EnableConsumerTracer)
                    mqBusFactoryConfigurator.LinkActivityTracingContext();

                // register consumers here
                registerConsumers(busRegistrationContext, mqBusFactoryConfigurator);
            });
        }

        /// <summary>
        /// Register receive EndPoint and bind with type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="busRegistrationContext">Bus registration contex.</param>
        /// <param name="busFactoryConfigurator">Configuration factory.</param>
        /// <param name="endpointConsumerConfigurator">Configure consumer delegate.</param>
        /// <param name="amazonMqOptions">Force connection params</param>
        /// <param name="createSeparateQueue">True for Publish, false when only Send.</param>
        /// <param name="serviceName">Unique service name prefix. If null will be entry point assembly name.</param>
        /// <typeparam name="T">Consumer type.</typeparam>
        /// <typeparam name="TMessage">Consumer message type.</typeparam>
        /// <exception cref="ArgumentNullException"/>
        public static void RegisterReceiveEndpoint<T, TMessage>(this IBusRegistrationContext busRegistrationContext,
            IAmazonSqsBusFactoryConfigurator busFactoryConfigurator,
            Action<IEndpointConfigurator>? endpointConsumerConfigurator = null, AmazonMqOptions? amazonMqOptions = null,
            bool createSeparateQueue = false, string? serviceName = null)
            where T : class, IConsumer<TMessage>
            where TMessage : class, IConsumer
        {
            if (busRegistrationContext == null) throw new ArgumentNullException(nameof(busRegistrationContext));
            if (busFactoryConfigurator == null) throw new ArgumentNullException(nameof(busFactoryConfigurator));

            RegisterConsumersEndpoint<TMessage>(busRegistrationContext, busFactoryConfigurator,
                endpointConsumerConfigurator, new[] { typeof(T) }, amazonMqOptions, createSeparateQueue, serviceName);
        }

        /// <summary>
        /// Configure recieve endpoint for Fifo queues.
        /// </summary>
        /// <param name="busRegistrationContext">Bus registration context</param>
        /// <param name="busFactoryConfigurator">Bus configuration context</param>
        /// <param name="endpointConsumer">Endpoint configuration context</param>
        /// <param name="amazonMqOptions">Force connection params</param>
        /// <param name="createSeparateQueue">Create separate Queue for consumer. It is allow to process same type messages with different consumers. It is publish-consumer pattern.</param>
        /// <param name="serviceName">Unique service name prefix. If null will be entry point assembly name.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ConfigurationException"/>
        public static void RegisterReceiveEndpointAsFifo<T, TMessage>(this IBusRegistrationContext busRegistrationContext,
            IAmazonSqsBusFactoryConfigurator busFactoryConfigurator,
            Action<IEndpointConfigurator>? endpointConsumer = null, AmazonMqOptions? amazonMqOptions = null,
            bool createSeparateQueue = false, string? serviceName = null)
            where T : class, IConsumer<TMessage>
            where TMessage : class
        {
            if (busRegistrationContext == null) throw new ArgumentNullException(nameof(busRegistrationContext));
            if (busFactoryConfigurator == null) throw new ArgumentNullException(nameof(busFactoryConfigurator));

            RegisterConsumersEndpoint<TMessage>(busRegistrationContext, busFactoryConfigurator, configurator =>
            {
                configurator.ConfigureConsumeTopology = false;
                configurator.QueueAttributes.Add(QueueAttributeName.FifoQueue, true);

                endpointConsumer?.Invoke(configurator);
            }, new[] { typeof(T) }, amazonMqOptions, createSeparateQueue, serviceName);
        }

        /// <summary>
        /// Register and bind SendEndpoint for type <typeparamref name="TMessage"/>.
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="amazonMqOptions">Force connection params.</param>
        /// <exception cref="ArgumentNullException"/>
        public static UriBuilder RegisterSendEndPoint<TMessage>(this IServiceProvider provider, AmazonMqOptions? amazonMqOptions = null)
            where TMessage : class
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));

            var endPoint = CreateQueueNameFromType<TMessage>(provider, amazonMqOptions);
            if (!EndpointConvention.TryGetDestinationAddress<TMessage>(out _))
            {
                EndpointConvention.Map<TMessage>(endPoint.Uri);
            }

            return endPoint;
        }

        /// <summary>
        /// Configure send params for FIFO queues.
        /// </summary>
        /// <param name="sendPipeline">Send pipeline configurator.</param>
        /// <param name="types">Types for fifo send approach.</param>
        /// <exception cref="ArgumentNullException"/>
        public static void ConfigureSendEndpointAsFifoForTypes(this ISendPipelineConfigurator sendPipeline, IEnumerable<Type> types)
        {
            if (sendPipeline == null) throw new ArgumentNullException(nameof(sendPipeline));
            if (types == null) throw new ArgumentNullException(nameof(types));

            sendPipeline.ConfigureSend(configurator =>
                configurator.UseSendExecute(context =>
                {
                    if (context.GetType().GenericTypeArguments.NullSafeAny(types.Contains))
                    {
                        // для упорядочевания внутри группы, aws упорядочевает только внутри группы
                        context.SetGroupId("68FE43E8-B5F1-4FB4-83DF-7A6B35F19C53");

                        // ключ уникальности
                        if (context.CorrelationId != null)
                        {
                            context.SetDeduplicationId(context.CorrelationId.ToString());
                        }
                    }
                }));
        }

        // private methods 

        private static UriBuilder CreateQueueNameFromType<TMessage>(this IServiceProvider provider, AmazonMqOptions? sqsOptions = null)
            where TMessage : class
        {
            var queueName = typeof(TMessage).Name.ReplaceRegex("(?i)dto(?-i)$", "");

            static AmazonMqOptions ValidateAwsOptions(AmazonMqOptions sqsOptions)
            {
                if (string.IsNullOrEmpty(sqsOptions.Region))
                {
                    throw new InvalidDataException("sqsOptions.Value.Region can't be empty");
                }

                if (string.IsNullOrEmpty(sqsOptions.OwnerId))
                {
                    throw new InvalidDataException("sqsOptions.Value.OwnerId can't be empty");
                }

                return sqsOptions;
            }

            sqsOptions ??= ValidateAwsOptions(sqsOptions ?? provider.GetRequiredService<IOptions<AmazonMqOptions>>().Value);
            // ReSharper disable once StringLiteralTypo
            var host = $"{AmazonSqsHostAddress.AmazonSqsScheme}://sqs.{sqsOptions.Region}.amazonaws.com";
            var path = $"/{sqsOptions.OwnerId}/{queueName.ReplaceRegex("fifo$", ".fifo")}";

            return new UriBuilder(host + path);
        }

        private static void RegisterConsumersEndpoint<TMessage>(IRegistrationContext busRegistrationContext,
            IAmazonSqsBusFactoryConfigurator busFactoryConfigurator,
            Action<IAmazonSqsReceiveEndpointConfigurator>? endpointConsumerConfigurator, IEnumerable<Type> types,
            AmazonMqOptions? amazonMqOptions = null, bool createSeparateQueue = false, string? serviceName = null)
            where TMessage : class
        {
            var endPoint = createSeparateQueue
                ? busRegistrationContext.CreateQueueNameFromType<TMessage>(amazonMqOptions)
                : busRegistrationContext.RegisterSendEndPoint<TMessage>(amazonMqOptions);

            if (!endPoint.Path.Contains(".fifo", StringComparison.Ordinal))
            {
                throw new ConfigurationException("AWS: DTO for FIFO queue must be end on [<Name>Fifo], example CommandProcessingFifo");
            }

            foreach (var consumerType in types)
            {
                var queueName = createSeparateQueue
                    ? endPoint.Uri.GetName(consumerType, serviceName)
                    : endPoint.Uri.GetName();

                busFactoryConfigurator.ReceiveEndpoint(queueName, configurator =>
                {
                    // https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SetQueueAttributes.html
                    configurator.PrefetchCount = 10; // max 10
                    configurator.WaitTimeSeconds = 20; // enable long pooling requests, range [0-20] 

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