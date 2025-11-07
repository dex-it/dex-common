using System;
using System.Collections.Generic;
using System.Linq;
using Dex.Cap.Outbox.Exceptions;

namespace Dex.Cap.Outbox.Interfaces;

public interface IOutboxMessage
{
    private static Dictionary<string, Type>? _currentDomainOutboxMessageTypesCache;

    /// <summary>
    /// Уникальный id типа сообщения в аутбоксе
    /// Используется для сопоставления входящих сообщений и принимающими обработчиками
    /// </summary>
    static abstract string OutboxMessageType { get; }

    static Dictionary<string, Type> CurrentDomainOutboxMessageTypes
    {
        get
        {
            _currentDomainOutboxMessageTypesCache ??= LoadCurrentDomainOutboxMessageTypes();
            return _currentDomainOutboxMessageTypesCache;

            Dictionary<string, Type> LoadCurrentDomainOutboxMessageTypes()
            {
                var messageTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => assembly.GetTypes())
                    .Where(t => typeof(IOutboxMessage).IsAssignableFrom(t) && t is {IsAbstract: false, IsInterface: false});

                return messageTypes
                    .Select(type =>
                    {
                        var discriminatorProperty = type.GetProperty(nameof(IOutboxMessage.OutboxMessageType));

                        if (discriminatorProperty?.GetValue(null) is not string discriminator || string.IsNullOrEmpty(discriminator))
                            throw new DiscriminatorResolveException($"Для сообщения аутбокса {type.FullName} не задан дискриминатор");

                        return new KeyValuePair<string, Type>(discriminator, type);
                    })
                    .ToDictionary();
            }
        }
    }
}