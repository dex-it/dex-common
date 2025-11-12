using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Exceptions;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox;

internal sealed class OutboxTypeDiscriminatorProvider(
    IServiceProvider serviceProvider,
    ILogger<OutboxTypeDiscriminatorProvider> logger) : IOutboxTypeDiscriminatorProvider
{
    private FrozenDictionary<string, Type>? _currentDomainOutboxMessageTypes;
    private FrozenSet<string>? _supportedDiscriminators;

    public async Task<FrozenSet<string>> GetSupportedDiscriminators()
    {
        _supportedDiscriminators ??= (await SupportedDiscriminatorsInternal()).ToFrozenSet();
        return _supportedDiscriminators;

        async Task<string[]> SupportedDiscriminatorsInternal()
        {
            var result = new List<string>(CurrentDomainOutboxMessageTypes.Count);

            await using var scope = serviceProvider.CreateAsyncScope();

            foreach (var (id, discriminatorType) in CurrentDomainOutboxMessageTypes)
            {
                var handlerType = typeof(IOutboxMessageHandler<>).MakeGenericType(discriminatorType);
                var handler = scope.ServiceProvider.GetService(handlerType);

                // нет подходящего хендлера
                if (handler is null)
                {
                    logger.LogWarning("Для сообщения {DiscriminatorId} нет подходящего хендлера", id);
                    continue;
                }

                // для сообщения есть подходящий автопаблишер, но сообщение не поддерживает автоматическую публикацию
                if (IsMessageAllowingAutoPublish(discriminatorType) is false && HandlerIsAutoPublisher(handler))
                {
                    logger.LogWarning("Для сообщения {DiscriminatorId} есть подходящий автопаблишер, но сообщение не поддерживает автоматическую публикацию", id);
                    continue;
                }

                // текущий сервис поддерживает обработку этого сообщения
                result.Add(id);
                logger.LogInformation("Текущий сервис поддерживает обработку сообщения {DiscriminatorId}", id);
            }

            return result.ToArray();
        }
    }

    public FrozenDictionary<string, Type> CurrentDomainOutboxMessageTypes
    {
        get
        {
            _currentDomainOutboxMessageTypes ??= LoadCurrentDomainOutboxMessageTypes().ToFrozenDictionary();
            return _currentDomainOutboxMessageTypes;

            Dictionary<string, Type> LoadCurrentDomainOutboxMessageTypes()
            {
                var messageTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.IsDynamic is false)
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => typeof(IOutboxMessage).IsAssignableFrom(t) && t is {IsAbstract: false, IsInterface: false, ContainsGenericParameters: false});

                return messageTypes
                    .Select(type =>
                    {
                        var discriminatorProperty = type.GetProperty(nameof(IOutboxMessage.OutboxTypeId));

                        if (discriminatorProperty?.GetValue(Activator.CreateInstance(type)) is not string discriminator || string.IsNullOrEmpty(discriminator))
                            throw new DiscriminatorResolveException($"Для сообщения аутбокса {type.FullName} не задан дискриминатор");

                        return new KeyValuePair<string, Type>(discriminator, type);
                    })
                    .ToDictionary();
            }
        }
    }

    /// <summary>
    /// Поддерживает ли сообщение автопаблиш
    /// </summary>
    private static bool IsMessageAllowingAutoPublish(Type message)
    {
        const string messageAllowAutoPublishPropertyName = nameof(IOutboxMessage.AllowAutoPublishing);

        var messageAllowAutoPublishProperty = message.GetProperty(messageAllowAutoPublishPropertyName);

        // base value if not override
        messageAllowAutoPublishProperty ??= typeof(IOutboxMessage).GetProperty(messageAllowAutoPublishPropertyName);

        if (messageAllowAutoPublishProperty is null)
            throw new InvalidOperationException($"Не удалось получить свойство {messageAllowAutoPublishPropertyName}, проверьте совместимость версий пакетов");

        var messageAllowAutoPublishValue = messageAllowAutoPublishProperty.GetValue(Activator.CreateInstance(message));

        return messageAllowAutoPublishValue is bool messageAllowAutoPublish
            ? messageAllowAutoPublish
            : throw new InvalidOperationException($"Не удалось получить значение свойства {messageAllowAutoPublishPropertyName}");
    }

    /// <summary>
    /// Является ли хендлер автопаблишером
    /// </summary>
    private static bool HandlerIsAutoPublisher(object handler)
    {
        const string isAutoPublisherPropertyName = nameof(IOutboxMessageHandler<OutboxMessageExample>.IsAutoPublisher);

        var handlerIsAutoPublisherProperty = handler
            .GetType()
            .GetProperty(isAutoPublisherPropertyName);

        // base value if not override
        handlerIsAutoPublisherProperty ??= typeof(IOutboxMessageHandler<OutboxMessageExample>).GetProperty(isAutoPublisherPropertyName);

        if (handlerIsAutoPublisherProperty is null)
            throw new InvalidOperationException($"Не удалось получить свойство {isAutoPublisherPropertyName}, проверьте совместимость версий пакетов");

        var handlerIsAutoPublisherValue = handlerIsAutoPublisherProperty.GetValue(null);

        return handlerIsAutoPublisherValue is bool handlerIsAutoPublisher
            ? handlerIsAutoPublisher
            : throw new InvalidOperationException($"Не удалось получить значение свойства {isAutoPublisherPropertyName}");
    }

    //todo: убрать и заменить на nameof(IOutboxMessageHandler<>.IsAutoPublisher после обновления до net10
    private abstract class OutboxMessageExample : IOutboxMessage
    {
        public string OutboxTypeId => string.Empty;
    }
}