using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.SQS;
using Dex.Extensions;
using MassTransit;
using MassTransit.AmazonSqsTransport;
using MassTransit.ExtensionsDependencyInjectionIntegration;
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
        /// Register bus.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public static void RegisterBus(this IServiceCollectionBusConfigurator collectionConfigurator,
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

                // mqBusFactoryConfigurator.ConnectConsumeObserver(busRegistrationContext.GetRequiredService<LogIdMassTransitObserver>()); // TODO

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
        /// <param name="isForPublish">True for Publish, false when only Send.</param>
        /// <typeparam name="T">Consumer type.</typeparam>
        /// <typeparam name="TMessage">Consumer message type.</typeparam>
        /// <exception cref="ArgumentNullException"/>
        public static void RegisterReceiveEndpoint<T, TMessage>(this IBusRegistrationContext busRegistrationContext, IAmazonSqsBusFactoryConfigurator busFactoryConfigurator,
            Action<IEndpointConfigurator>? endpointConsumerConfigurator = null, bool isForPublish = false)
            where T : class, IConsumer<TMessage>
            where TMessage : class, IConsumer, new()
        {
            if (busRegistrationContext == null) throw new ArgumentNullException(nameof(busRegistrationContext));
            if (busFactoryConfigurator == null) throw new ArgumentNullException(nameof(busFactoryConfigurator));

            RegisterConsumersEndpoint<TMessage>(busRegistrationContext, busFactoryConfigurator, endpointConsumerConfigurator, new[] {typeof(T)}, isForPublish);
        }

        /// <summary>
        /// Configure recieve endpoint for Fifo queues.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ConfigurationException"/>
        public static void RegisterReceiveEndpointAsFifo<T, TMessage>(this IBusRegistrationContext busRegistrationContext, IAmazonSqsBusFactoryConfigurator busFactoryConfigurator,
            Action<IEndpointConfigurator>? endpointConsumer = null, bool isForPublish = false)
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
            }, new[] {typeof(T)}, isForPublish);
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

        /// <summary>
        /// Configure send params for FIFO queues.
        /// </summary>
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

        private static UriBuilder CreateQueueNameFromType<TMessage>(this IServiceProvider provider) 
            where TMessage : class
        {
            var queueName = typeof(TMessage).Name.ToLowerInvariant().ReplaceRegex("dto$", "");

            static AmazonMqOptions ValidateAwsOptions(IOptions<AmazonMqOptions> sqsOptions)
            {
                if (string.IsNullOrEmpty(sqsOptions.Value.Region))
                {
                    throw new InvalidDataException("sqsOptions.Value.Region can't be empty");
                }

                if (string.IsNullOrEmpty(sqsOptions.Value.OwnerId))
                {
                    throw new InvalidDataException("sqsOptions.Value.OwnerId can't be empty");
                }

                return sqsOptions.Value;
            }

            var sqsOptions = ValidateAwsOptions(provider.GetRequiredService<IOptions<AmazonMqOptions>>());
            // ReSharper disable once StringLiteralTypo
            var host = $"{AmazonSqsHostAddress.AmazonSqsScheme}://sqs.{sqsOptions.Region}.amazonaws.com";
            var path = $"/{sqsOptions.OwnerId}/{queueName.ReplaceRegex("fifo$", ".fifo")}";

            return new UriBuilder(host + path);
        }

        private static void RegisterConsumersEndpoint<TMessage>(IRegistration busRegistrationContext, IAmazonSqsBusFactoryConfigurator busFactoryConfigurator,
            Action<IAmazonSqsReceiveEndpointConfigurator>? endpointConsumerConfigurator, IEnumerable<Type> types, bool isForPublish = false) 
            where TMessage : class
        {
            var endPoint = isForPublish ? busRegistrationContext.CreateQueueNameFromType<TMessage>() : busRegistrationContext.RegisterSendEndPoint<TMessage>();
            if (!endPoint.Path.Contains(".fifo", StringComparison.Ordinal))
            {
                throw new ConfigurationException("AWS: DTO for FIFO queue must be end on [<Name>Fifo], example CommandProcessingFifo");
            }

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