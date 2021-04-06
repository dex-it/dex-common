using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MassTransit;

namespace Dex.MassTransit.Extensions
{
    public static class QueueMassTransitConfigurator
    {
        public static QueueType QueueType = QueueType.Rabbit;

        /// <summary>
        /// Подключение к шине
        /// </summary>
        /// <param name="collectionConfigurator"></param>
        /// <param name="registerConsumers"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterBus( this IServiceCollectionConfigurator collectionConfigurator,  Action<IServiceProvider, IBusFactoryConfigurator> registerConsumers)
        {
            if (collectionConfigurator == null) throw new ArgumentNullException(nameof(collectionConfigurator));
            if (registerConsumers == null) throw new ArgumentNullException(nameof(registerConsumers));

            switch (QueueType)
            {
                case QueueType.Rabbit:
                    collectionConfigurator.AddBus(registrationContext => CreateRabbitMqBusControl(registerConsumers, registrationContext.Container));
                    break;
                case QueueType.AmazonSQS:
                    collectionConfigurator.AddBus(registrationContext => CreateAmazonSqsBusControl(registerConsumers, registrationContext.Container));
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private static IBusControl CreateAmazonSqsBusControl(Action<IServiceProvider, IBusFactoryConfigurator> registerConsumers, IServiceProvider provider)
        {
            var busControl = Bus.Factory.CreateUsingAmazonSqs(cfg =>
            {
                var mqSettings = provider.GetService<IOptions<AmazonMqOptions>>().Value;
                cfg.Host(mqSettings.Region, configurator =>
                {
                    configurator.AccessKey(mqSettings.AccessKey);
                    configurator.SecretKey(mqSettings.SecretKey);
                });

                cfg.AutoDelete = false;
                cfg.Durable = true;

                // register consumers here
                registerConsumers(provider, cfg);
            });

            busControl.ConnectConsumeObserver(provider.GetService<LogIdMassTransitObserver>());

            return busControl;
        }

        private static IBusControl CreateRabbitMqBusControl(Action<IServiceProvider, IBusFactoryConfigurator> registerConsumers, IServiceProvider provider)
        {
            var busControl = Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var mqSettings = provider.GetService<IOptions<RabbitMqOptions>>().Value;
                cfg.Host(mqSettings.Host, mqSettings.Port, mqSettings.VHost, $"{Environment.MachineName}:{AppDomain.CurrentDomain.FriendlyName}");

                cfg.AutoDelete = false;
                cfg.Durable = true;

                // register consumers here
                registerConsumers(provider, cfg);
            });

            busControl.ConnectConsumeObserver(provider.GetService<LogIdMassTransitObserver>());

            return busControl;
        }

        /// <summary>
        /// Для получения сообщений типа T, после этого все сообщения типа T будут приниматься,
        /// также автоматически регистрируется EndPoint и выполняется привязка 
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="cfg"></param>
        /// <param name="endpointConsumer">позволяет настроить режим работы consumer</param>
        /// <param name="isForPublish">true когда регистрируем точку для Publish, false когда для Send</param>
        /// <typeparam name="TD">Consumer message type</typeparam>
        /// <typeparam name="T">Consumer type</typeparam>
        /// <typeparam name="T1">Consumer type</typeparam>
        /// <typeparam name="T2">Consumer type</typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterReceiveEndpoint<T, T1, T2, TD>(this IServiceProvider provider,  IBusFactoryConfigurator cfg,
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
        /// Для получения сообщений типа T, после этого все сообщения типа T будут приниматься,
        /// также автоматически регистрируется EndPoint и выполняется привязка 
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="cfg"></param>
        /// <param name="endpointConsumer">позволяет настроить режим работы consumer</param>
        /// <param name="isForPublish">true когда регистрируем точку для Publish, false когда для Send</param>
        /// <typeparam name="TD">Consumer message type</typeparam>
        /// <typeparam name="T">Consumer type</typeparam>
        /// <typeparam name="T1">Consumer type</typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterReceiveEndpoint<T, T1, TD>(this IServiceProvider provider,  IBusFactoryConfigurator cfg,
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
        /// Для получения сообщений типа T, после этого все сообщения типа T будут приниматься,
        /// также автоматически регистрируется EndPoint и выполняется привязка 
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="cfg"></param>
        /// <param name="endpointConsumer">позволяет настроить режим работы consumer</param>
        /// <param name="isForPublish">true когда регистрируем точку для Publish, false когда для Send</param>
        /// <typeparam name="TD">Consumer message type</typeparam>
        /// <typeparam name="T">Consumer type</typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void RegisterReceiveEndpoint<T, TD>(this IServiceProvider provider,  IBusFactoryConfigurator cfg,
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
        public static void RegisterReceiveEndpointAsFifo<T, TD>(this IServiceProvider provider,  IBusFactoryConfigurator cfg,
             Action<IEndpointConfigurator> endpointConsumer = null, bool isForPublish = false)
            where T : class, IConsumer<TD>
            where TD : class, IConsumer, new()
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));

            var endPoint = isForPublish ? provider.CreateQueueNameFromType<TD>() : provider.RegisterSendEndPoint<TD>();

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

        private static void RegisterConsumersEndpoint(IServiceProvider provider, IBusFactoryConfigurator cfg, UriBuilder endPoint,
            Action<IEndpointConfigurator> endpointConsumerConfigurator, Type[] types, bool isForPublish = false)
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
                        configurator.UseMessageRetry(x => { x.SetRetryPolicy(filter => filter.Interval(256, 1.Minutes())); });
                    }
                    else
                    {
                        endpointConsumerConfigurator.Invoke(configurator);
                    }

                    configurator.ConfigureConsumer(provider, types);
                });
            }
        }

        /// <summary>
        /// Configure send params for FIFO queues
        /// </summary>
        /// <param name="sendPipeline"></param>
        /// <param name="types"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void ConfigureSendEndpointAsFifoForTypes( this ISendPipelineConfigurator sendPipeline,  IEnumerable<Type> types)
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
        /// Для отправки сообщений необходимо к типу T, создать и привязать EndPoint
        /// </summary>
        /// <param name="provider"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static UriBuilder RegisterSendEndPoint<T>( this IServiceProvider provider) where T : class, IConsumer, new()
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
                    var mqOptions = provider.GetService<IOptions<RabbitMqOptions>>();
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
                    var host = $"{AmazonSqsHostAddress.AmazonSqsScheme}://sqs.{sqsOptions.Region}.amazonaws.com";
                    var path = $"/{sqsOptions.OwnerId}/{queueName.ReplaceRegex("fifo$", ".fifo")}";

                    return new UriBuilder(host + path);
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public enum QueueType
    {
        Rabbit,

        // ReSharper disable once InconsistentNaming
        AmazonSQS
    }
}