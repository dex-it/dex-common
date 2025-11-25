using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Outbox;

internal sealed class OutboxTypeDiscriminatorProvider(
    IServiceProvider serviceProvider,
    ILogger<OutboxTypeDiscriminatorProvider> logger) : IOutboxTypeDiscriminatorProvider
{
    private FrozenDictionary<string, Type>? _currentDomainOutboxMessageTypes;
    private ImmutableArray<string>? _immediatelyDeletableMessages;
    private FrozenSet<string>? _supportedDiscriminators;

    public FrozenSet<string> SupportedDiscriminators
    {
        get
        {
            _supportedDiscriminators ??= GetSupportedDiscriminatorsInternal().ToFrozenSet();
            return _supportedDiscriminators;

            string[] GetSupportedDiscriminatorsInternal()
            {
                var result = new List<string>(CurrentDomainOutboxMessageTypes.Count);

                using var scope = serviceProvider.CreateScope();

                foreach (var (id, discriminatorType) in CurrentDomainOutboxMessageTypes)
                {
                    try
                    {
                        var handlerType = typeof(IOutboxMessageHandler<>).MakeGenericType(discriminatorType);
                        var handler = scope.ServiceProvider.GetService(handlerType);

                        // нет подходящего хендлера
                        if (handler is null)
                        {
                            logger.LogWarning("Для сообщения {DiscriminatorId} - {MessageType} нет подходящего хендлера", id, discriminatorType.FullName);
                            continue;
                        }

                        // для сообщения есть подходящий автопаблишер, но сообщение не поддерживает автоматическую публикацию
                        if (IsMessageAllowingAutoPublish(discriminatorType) is false && IsAutoPublisherHandler(handler))
                        {
                            logger.LogWarning(
                                "Для сообщения {DiscriminatorId} - {MessageType} есть подходящий автопаблишер, но сообщение не поддерживает автоматическую публикацию",
                                id,
                                discriminatorType.FullName);
                            continue;
                        }

                        // текущий сервис поддерживает обработку этого сообщения
                        result.Add(id);
                        logger.LogInformation("Текущий сервис поддерживает обработку сообщения {DiscriminatorId} - {MessageType}", id, discriminatorType.FullName);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Не удалось зарегистрировать сообщение {DiscriminatorId} - {MessageType}", id, discriminatorType.FullName);
                    }
                }

                return result.ToArray();
            }
        }
    }

    public ImmutableArray<string> ImmediatelyDeletableMessages
    {
        get
        {
            _immediatelyDeletableMessages ??= GetImmediatelyDeletableMessages();
            return _immediatelyDeletableMessages.Value;

            ImmutableArray<string> GetImmediatelyDeletableMessages()
            {
                var result = new List<string>(CurrentDomainOutboxMessageTypes.Count);

                foreach (var (id, discriminatorType) in CurrentDomainOutboxMessageTypes)
                {
                    try
                    {
                        if (IsMessageImmediatelyDeletable(discriminatorType) is false) continue;

                        logger.LogInformation("Сообщения {DiscriminatorId} - {MessageType} будут удаляться сразу после обработки", id, discriminatorType.FullName);
                        result.Add(id);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Не удалось узнать признак немедленного удаления сообщения {DiscriminatorId} - {MessageType}", id, discriminatorType.FullName);
                    }
                }

                return [..result];
            }
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

                        if (discriminatorProperty?.GetValue(null) is not string discriminator || string.IsNullOrWhiteSpace(discriminator))
                        {
                            logger.LogError("Для сообщения аутбокса {TypeName} не задан дискриминатор", type.FullName);
                            discriminator = string.Empty;
                        }

                        return new KeyValuePair<string, Type>(discriminator, type);
                    })
                    .Where(x => string.IsNullOrWhiteSpace(x.Key) is false)
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

        var messageAllowAutoPublishValue = messageAllowAutoPublishProperty.GetValue(null);

        return messageAllowAutoPublishValue is bool messageAllowAutoPublish
            ? messageAllowAutoPublish
            : throw new InvalidOperationException($"Не удалось получить значение свойства {messageAllowAutoPublishPropertyName}");
    }

    /// <summary>
    /// Требует ли сообщение немедленного удаления после обработки
    /// </summary>
    private static bool IsMessageImmediatelyDeletable(Type message)
    {
        const string messageImmediatelyDeletablePropertyName = nameof(IOutboxMessage.DeleteImmediately);

        var messageImmediatelyDeletableProperty = message.GetProperty(messageImmediatelyDeletablePropertyName);

        // base value if not override
        messageImmediatelyDeletableProperty ??= typeof(IOutboxMessage).GetProperty(messageImmediatelyDeletablePropertyName);

        if (messageImmediatelyDeletableProperty is null)
            throw new InvalidOperationException($"Не удалось получить свойство {messageImmediatelyDeletablePropertyName}, проверьте совместимость версий пакетов");

        var immediatelyDeletableValue = messageImmediatelyDeletableProperty.GetValue(null);

        return immediatelyDeletableValue is bool immediatelyDeletable
            ? immediatelyDeletable
            : throw new InvalidOperationException($"Не удалось получить значение свойства {messageImmediatelyDeletablePropertyName}");
    }

    /// <summary>
    /// Является ли хендлер автопаблишером
    /// </summary>
    private static bool IsAutoPublisherHandler(object handler)
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
        public static string OutboxTypeId => nameof(OutboxMessageExample);
    }
}