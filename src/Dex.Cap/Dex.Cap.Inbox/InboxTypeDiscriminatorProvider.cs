using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dex.Cap.Inbox.Exceptions;
using Dex.Cap.Inbox.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Inbox;

internal sealed class InboxTypeDiscriminatorProvider(IServiceProvider serviceProvider, ILogger<InboxTypeDiscriminatorProvider> logger)
    : IInboxTypeDiscriminatorProvider
{
    private FrozenDictionary<string, Type>? _currentDomainInboxMessageTypes;
    private FrozenSet<string>? _supportedDiscriminators;

    public FrozenDictionary<string, Type> CurrentDomainInboxMessageTypes =>
        _currentDomainInboxMessageTypes ??= DiscoverMessageTypes().ToFrozenDictionary();

    public FrozenSet<string> SupportedDiscriminators =>
        _supportedDiscriminators ??= DiscoverSupportedDiscriminators().ToFrozenSet();

    private Dictionary<string, Type> DiscoverMessageTypes()
    {
        var messageTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.IsDynamic is false)
            .SelectMany(GetLoadableTypes)
            .Where(t => typeof(IInboxMessage).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false });

        var result = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var type in messageTypes)
        {
            var discriminator = GetDiscriminator(type);

            if (result.TryGetValue(discriminator, out var existingType))
            {
                // Молчаливо выбрать один из типов нельзя: сохранённое сообщение перестанет
                // однозначно восстанавливаться, и обработка уйдёт не в тот обработчик.
                throw new DiscriminatorConflictException(
                    $"Дискриминатор '{discriminator}' объявлен несколькими типами сообщений инбокса: " +
                    $"'{existingType.FullName}' и '{type.FullName}'. Дискриминатор обязан быть уникальным.");
            }

            result.Add(discriminator, type);
        }

        return result;
    }

    private HashSet<string> DiscoverSupportedDiscriminators()
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        using var scope = serviceProvider.CreateScope();

        foreach (var (discriminator, messageType) in CurrentDomainInboxMessageTypes)
        {
            var handlerType = typeof(IInboxMessageHandler<>).MakeGenericType(messageType);
            var handler = scope.ServiceProvider.GetService(handlerType);

            if (handler is null)
            {
                logger.LogWarning(
                    "No handler is registered for inbox message {DiscriminatorId} - {MessageType}, it will not be processed by this service",
                    discriminator, messageType.FullName);
                continue;
            }

            result.Add(discriminator);
            logger.LogInformation(
                "Current service handles inbox message {DiscriminatorId} - {MessageType}",
                discriminator, messageType.FullName);
        }

        return result;
    }

    private static string GetDiscriminator(Type type)
    {
        // static abstract член виден только на самом конкретном типе: GetProperty не ищет по иерархии.
        var discriminatorProperty = type.GetProperty(nameof(IInboxMessage.InboxTypeId), BindingFlags.Public | BindingFlags.Static);

        if (discriminatorProperty?.GetValue(null) is not string discriminator || string.IsNullOrWhiteSpace(discriminator))
        {
            throw new DiscriminatorConflictException(
                $"Тип сообщения инбокса '{type.FullName}' не задал {nameof(IInboxMessage.InboxTypeId)}. " +
                "Дискриминатор обязан быть непустым и стабильным.");
        }

        return discriminator;
    }

    private IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            // Частично загружаемая сборка не должна валить дискавери целиком: берём то, что загрузилось.
            logger.LogWarning(e, "Assembly {Assembly} is partially loadable, inbox message types may be incomplete", assembly.FullName);
            return e.Types.Where(t => t is not null)!;
        }
    }
}
