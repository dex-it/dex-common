using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.SQS;
using Dex.Extensions;
using Dex.MassTransit.Extensions.Options;
using MassTransit;
using MassTransit.AmazonSqsTransport;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Dex.MassTransit.Extensions
{
    public static class QueueMassTransitConfigurator
    {
        public static QueueType QueueType = QueueType.Rabbit;

        public static void RegisterBus(this IServiceCollectionBusConfigurator collectionConfigurator, Action<IBusRegistrationContext, IBusFactoryConfigurator> registerConsumers)
        {
            if (collectionConfigurator == null) throw new ArgumentNullException(nameof(collectionConfigurator));
            if (registerConsumers == null) throw new ArgumentNullException(nameof(registerConsumers));

            switch (QueueType)
            {
                case QueueType.Rabbit:
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
                    break;
                case QueueType.AmazonSQS:
                    collectionConfigurator.UsingAmazonSqs((busRegistrationContext, mqBusFactoryConfigurator) =>
                    {
                        var amazonMqOptions = busRegistrationContext.GetRequiredService<IOptions<AmazonMqOptions>>().Value;
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
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Register recieve EndPoint and bind with type T
        /// </summary>
        /// <param name="provider">bus registration contex</param>
        /// <param name="cfg">configuration factory</param>
        /// <param name="endpointConsumer">configure consumer delegate</param>
        /// <param name="isForPublish">true for Publish, false when only Send</param>
        /// <typeparam name="Td">Consumer message type</typeparam>
        /// <typeparam name="T">Consumer type</typeparam>
        /// <typeparam name="T1">Consumer type</typeparam>
        /// <typeparam name="T2">Consumer type</typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterReceiveEndpoint<T, T1, T2, Td>(this IBusRegistrationContext provider, IBusFactoryConfigurator cfg,
            Action<IEndpointConfigurator> endpointConsumer = null, bool isForPublish = true)
            where T : class, IConsumer<Td>
            where Td : class, IConsumer, new()
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));

            var endPoint = isForPublish ? provider.CreateQueueNameFromType<Td>() : provider.RegisterSendEndPoint<Td>();
            RegisterConsumersEndpoint(provider, cfg, endPoint, endpointConsumer, new[] {typeof(T), typeof(T1), typeof(T2)}, isForPublish);
        }

        /// <summary>
        /// Register recieve EndPoint and bind with type T
        /// </summary>
        /// <param name="provider">bus registration contex</param>
        /// <param name="cfg">configuration factory</param>
        /// <param name="endpointConsumer">configure consumer delegate</param>
        /// <param name="isForPublish">true for Publish, false when only Send</param>
        /// <typeparam name="Td">Consumer message type</typeparam>
        /// <typeparam name="T">Consumer type</typeparam>
        /// <typeparam name="T1">Consumer type</typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterReceiveEndpoint<T, T1, Td>(this IBusRegistrationContext provider, IBusFactoryConfigurator cfg,
            Action<IEndpointConfigurator> endpointConsumer = null, bool isForPublish = true)
            where T : class, IConsumer<Td>
            where Td : class, IConsumer, new()
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));

            var endPoint = isForPublish ? provider.CreateQueueNameFromType<Td>() : provider.RegisterSendEndPoint<Td>();
            RegisterConsumersEndpoint(provider, cfg, endPoint, endpointConsumer, new[] {typeof(T), typeof(T1)}, isForPublish);
        }

        /// <summary>
        /// Register recieve EndPoint and bind with type T
        /// </summary>
        /// <param name="provider">bus registration contex</param>
        /// <param name="cfg">configuration factory</param>
        /// <param name="endpointConsumer">configure consumer delegate</param>
        /// <param name="isForPublish">true for Publish, false when only Send</param>
        /// <typeparam name="Td">Consumer message type</typeparam>
        /// <typeparam name="T">Consumer type</typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterReceiveEndpoint<T, Td>(this IBusRegistrationContext provider, IBusFactoryConfigurator cfg,
            Action<IEndpointConfigurator> endpointConsumer = null, bool isForPublish = false)
            where T : class, IConsumer<Td>
            where Td : class, IConsumer, new()
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));

            var endPoint = isForPublish ? provider.CreateQueueNameFromType<Td>() : provider.RegisterSendEndPoint<Td>();
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
        /// <typeparam name="Td"></typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ConfigurationException"></exception>
        public static void RegisterReceiveEndpointAsFifo<T, Td>(this IBusRegistrationContext provider, IBusFactoryConfigurator cfg,
            Action<IEndpointConfigurator> endpointConsumer = null, bool isForPublish = false)
            where T : class, IConsumer<Td>
            where Td : class, IConsumer, new()
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));

            var endPoint = isForPublish ? provider.CreateQueueNameFromType<Td>() : provider.RegisterSendEndPoint<Td>();

            if (QueueType == QueueType.AmazonSQS && !endPoint.Path.Contains(".fifo"))
            {
                throw new ConfigurationException("AWS: DTO for FIFO queue must be end on [<Name>Fifo], example CommandProcessingFifo");
            }

            RegisterConsumersEndpoint(provider, cfg, endPoint, configurator =>
            {
                if (configurator is IAmazonSqsReceiveEndpointConfigurator awssqs)
                {
                    awssqs.ConfigureConsumeTopology = false;
                    awssqs.QueueAttributes.Add(QueueAttributeName.FifoQueue, true);
                }

                endpointConsumer?.Invoke(configurator);
            }, new[] {typeof(T)}, isForPublish);
        }


        /// <summary>
        /// Configure send params for FIFO queues
        /// </summary>
        /// <param name="sendPipeline"></param>
        /// <param name="types"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void ConfigureSendEndpointAsFifoForTypes(this ISendPipelineConfigurator sendPipeline, IEnumerable<Type> types)
        {
            if (sendPipeline == null) throw new ArgumentNullException(nameof(sendPipeline));
            if (types == null) throw new ArgumentNullException(nameof(types));

            if (sendPipeline is IAmazonSqsBusFactoryConfigurator)
            {
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


        private static UriBuilder CreateQueueNameFromType<T>(this IServiceProvider provider) where T : class, IConsumer, new()
        {
            var queueName = typeof(T).Name.ToLower().ReplaceRegex("dto$", "");

            switch (QueueType)
            {
                case QueueType.Rabbit:
                    var mqOptions = provider.GetRequiredService<IOptions<RabbitMqOptions>>();
                    var endPoint = new UriBuilder("rabbitmq", mqOptions.Value.Host, mqOptions.Value.Port, queueName);
                    return endPoint;
                case QueueType.AmazonSQS:

                    static AmazonMqOptions ValidateAwsOptions(IOptions<AmazonMqOptions> sqsOptions)
                    {
                        if (sqsOptions.Value.Region.IsNullOrEmpty())
                        {
                            throw new InvalidDataException("sqsOptions.Value.Region can't be empty");
                        }

                        if (sqsOptions.Value.OwnerId.IsNullOrEmpty())
                        {
                            throw new InvalidDataException("sqsOptions.Value.OwnerId can't be empty");
                        }

                        return sqsOptions.Value;
                    }

                    var sqsOptions = ValidateAwsOptions(provider.GetService<IOptions<AmazonMqOptions>>());
                    // ReSharper disable once StringLiteralTypo
                    var host = $"{AmazonSqsHostAddress.AmazonSqsScheme}://sqs.{sqsOptions.Region}.amazonaws.com";
                    var path = $"/{sqsOptions.OwnerId}/{queueName.ReplaceRegex("fifo$", ".fifo")}";

                    return new UriBuilder(host + path);
                default:
                    throw new NotSupportedException();
            }
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
                    if (configurator is IAmazonSqsReceiveEndpointConfigurator awssqs)
                    {
                        // https://docs.aws.amazon.com/AWSSimpleQueueService/latest/APIReference/API_SetQueueAttributes.html
                        awssqs.PrefetchCount = 10; // max 10
                        awssqs.WaitTimeSeconds = 20; // enable long pooling requests, range [0-20] 
                    }

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