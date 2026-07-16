using System;
using System.Collections.Frozen;
using System.Collections.Generic;
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

    /// <remarks>
    /// Lazy, а не "??=": реестр читают конкурентно (приём из транспорта и фоновая обработка), а построение
    /// сканирует все загруженные сборки. ExecutionAndPublication гарантирует ровно одно построение и, что
    /// важнее, кэширует исключение: ошибка конфигурации не должна пересканировать сборки на каждом
    /// обращении, она обязана оставаться одной и той же ошибкой.
    /// </remarks>
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

    /// <summary>
    /// Построить реестр «дискриминатор - тип сообщения» по типам, известным этому домену.
    /// </summary>
    /// <remarks>
    /// Конфликт дискриминаторов это ошибка конфигурации, а не повод выбрать один из типов: молчаливый
    /// выбор лишил бы сохранённое сообщение однозначного восстановления, и обработка ушла бы не в тот
    /// обработчик.
    /// </remarks>
    /// <exception cref="DiscriminatorConflictException">Один дискриминатор заявлен несколькими типами.</exception>
    private Dictionary<string, Type> BuildRegistry()
    {
        var result = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var type in _typeSource.GetMessageTypes())
        {
            var discriminator = GetDiscriminator(type);

            if (result.TryGetValue(discriminator, out var existingType))
            {
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

    /// <summary>
    /// Прочитать дискриминатор, объявленный типом сообщения.
    /// </summary>
    /// <remarks>
    /// FlattenHierarchy обязателен: статический член, объявленный базовым классом, иначе не виден, и тип,
    /// унаследовавший дискриминатор, отвергался бы с сообщением «не задал InboxTypeId», хотя компилятор
    /// такой код принимает. Честный исход для такой иерархии это конфликт дискриминаторов: базовый и
    /// производный типы заявляют один и тот же идентификатор.
    /// <para>
    /// Набор символов намеренно не ограничен: дискриминатор уходит в SQL захвата параметром, а не
    /// подстановкой в текст запроса, поэтому ни кавычка, ни фигурная скобка запрос не ломают. Ограничение
    /// отвергало бы заведомо рабочие значения, например MessageUrn MassTransit ('urn:message:...') или имя
    /// вложенного типа ('Outer+Inner'), а сменить дискриминатор существующему типу нельзя: он лежит в БД.
    /// </para>
    /// </remarks>
    /// <exception cref="DiscriminatorResolveException">Тип не объявил непустой дискриминатор.</exception>
    private static string GetDiscriminator(Type type)
    {
        var discriminatorProperty = type.GetProperty(
            nameof(IInboxMessage.InboxTypeId),
            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        if (discriminatorProperty?.GetValue(null) is not string discriminator || string.IsNullOrWhiteSpace(discriminator))
        {
            throw new DiscriminatorResolveException(
                $"Inbox message type '{type.FullName}' does not define {nameof(IInboxMessage.InboxTypeId)}. " +
                "A discriminator must be non-empty and stable.");
        }

        return discriminator;
    }
}