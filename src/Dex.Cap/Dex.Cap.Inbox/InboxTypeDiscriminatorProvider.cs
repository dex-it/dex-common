using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Dex.Cap.Inbox.Exceptions;
using Dex.Cap.Inbox.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Inbox;

internal sealed class InboxTypeDiscriminatorProvider : IInboxTypeDiscriminatorProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInboxMessageTypeSource _typeSource;
    private readonly ILogger<InboxTypeDiscriminatorProvider> _logger;

    // Lazy, а не "??=": реестр читают конкурентно (приём из HTTP и фоновая обработка), а построение
    // сканирует все загруженные сборки. ExecutionAndPublication гарантирует ровно одно построение и,
    // что важнее, кэширует исключение: ошибка конфигурации не должна пересканировать сборки на каждом
    // обращении, она обязана оставаться одной и той же ошибкой.
    private readonly Lazy<FrozenDictionary<string, Type>> _currentDomainInboxMessageTypes;
    private readonly Lazy<FrozenSet<string>> _supportedDiscriminators;

    public InboxTypeDiscriminatorProvider(
        IServiceProvider serviceProvider,
        IInboxMessageTypeSource typeSource,
        ILogger<InboxTypeDiscriminatorProvider> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _typeSource = typeSource ?? throw new ArgumentNullException(nameof(typeSource));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _currentDomainInboxMessageTypes = new Lazy<FrozenDictionary<string, Type>>(
            () => BuildRegistry().ToFrozenDictionary(),
            LazyThreadSafetyMode.ExecutionAndPublication);

        _supportedDiscriminators = new Lazy<FrozenSet<string>>(
            () => DiscoverSupportedDiscriminators().ToFrozenSet(),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public FrozenDictionary<string, Type> CurrentDomainInboxMessageTypes => _currentDomainInboxMessageTypes.Value;

    public FrozenSet<string> SupportedDiscriminators => _supportedDiscriminators.Value;

    public void Warmup()
    {
        _ = SupportedDiscriminators;
    }

    private Dictionary<string, Type> BuildRegistry()
    {
        var result = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var type in _typeSource.GetMessageTypes())
        {
            var discriminator = GetDiscriminator(type);

            if (result.TryGetValue(discriminator, out var existingType))
            {
                // Молчаливо выбрать один из типов нельзя: сохранённое сообщение перестанет
                // однозначно восстанавливаться, и обработка уйдёт не в тот обработчик.
                throw new DiscriminatorConflictException(
                    $"Discriminator '{discriminator}' is declared by several inbox message types: " +
                    $"'{existingType.FullName}' and '{type.FullName}'. A discriminator must be unique.");
            }

            result.Add(discriminator, type);
        }

        return result;
    }

    private HashSet<string> DiscoverSupportedDiscriminators()
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        using var scope = _serviceProvider.CreateScope();

        foreach (var (discriminator, messageType) in CurrentDomainInboxMessageTypes)
        {
            var handlerType = typeof(IInboxMessageHandler<>).MakeGenericType(messageType);
            var handler = scope.ServiceProvider.GetService(handlerType);

            if (handler is null)
            {
                _logger.LogWarning(
                    "No handler is registered for inbox message {DiscriminatorId} - {MessageType}, it will not be processed by this service",
                    discriminator, messageType.FullName);
                continue;
            }

            result.Add(discriminator);
            _logger.LogInformation(
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
            throw new DiscriminatorResolveException(
                $"Inbox message type '{type.FullName}' does not define {nameof(IInboxMessage.InboxTypeId)}. " +
                "A discriminator must be non-empty and stable.");
        }

        // Дискриминатор подставляется в SQL выборки литералом, а сам SQL проходит через string.Format
        // внутри EF. Поэтому кавычка ломает запрос на стороне Postgres, а фигурная скобка ещё раньше, на
        // форматировании. И то, и другое падало бы на каждом цикле фоновой обработки, где превращается
        // в LogCritical при формально живом хосте, поэтому проверяем здесь, при построении реестра.
        //
        // Белый список, а не перечисление опасных символов: дискриминатор это стабильный идентификатор
        // типа (на практике GUID или имя), у него нет причин содержать что-то кроме букв, цифр, дефиса,
        // подчёркивания и точки. Запрещать по списку значит однажды забыть очередной символ.
        foreach (var symbol in discriminator)
        {
            if (char.IsAsciiLetterOrDigit(symbol) || symbol is '-' or '_' or '.')
            {
                continue;
            }

            throw new DiscriminatorResolveException(
                $"Inbox message type '{type.FullName}' defines {nameof(IInboxMessage.InboxTypeId)} " +
                $"'{discriminator}' containing the unsupported character '{symbol}'. " +
                "A discriminator may only contain ASCII letters, digits, '-', '_' and '.'.");
        }

        return discriminator;
    }
}
