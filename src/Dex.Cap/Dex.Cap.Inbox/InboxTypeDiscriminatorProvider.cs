using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
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
    /// <para>
    /// Повторно пришедший тот же тип не конфликт: отображение от него не меняется, выбирать не из чего.
    /// А два разных CLR-типа с одной идентичностью это вообще не про дискриминаторы: так выглядит сборка,
    /// загруженная в процесс несколько раз. Причина другая, и чинится она в хосте, поэтому и ошибка
    /// отдельная.
    /// </para>
    /// <para>
    /// Проверяются ВСЕ типы домена, а не только те, чей обработчик зарегистрирован, и сузить проверку до
    /// обслуживаемых нельзя. Набор обслуживаемых сам ВЫВОДИТСЯ из этого реестра
    /// (<see cref="DiscoverSupportedDiscriminators"/> идёт по его парам и спрашивает у DI обработчик под
    /// конкретный тип), поэтому при неразрешённом конфликте на дискриминаторе неизвестно даже то, обслуживаем
    /// ли мы его: ответ зависел бы от того, какой из двух типов победил. Реестр обязан быть однозначен ДО
    /// того, как на него можно опереться.
    /// </para>
    /// <para>
    /// Цена такой строгости честная: конфликт в чужой библиотеке роняет старт сервиса, даже если сам он эти
    /// сообщения не обслуживает. Обратная цена несоразмерна. Пусть сервис обслуживает M1 с дискриминатором X,
    /// а загруженная рядом библиотека объявляет M2 с тем же X. Проверка по обслуживаемым построила бы реестр
    /// X -> M1 и молча приняла бы такую конфигурацию, после чего выборка забирала бы строки с MessageType = X,
    /// а тело M2 читалось бы как M1. То есть громкий отказ на старте менялся бы на тихую обработку сообщения
    /// не тем обработчиком.
    /// </para>
    /// </remarks>
    /// <exception cref="DiscriminatorConflictException">Один дискриминатор заявлен несколькими типами.</exception>
    /// <exception cref="AmbiguousMessageTypeException">Тип сообщения загружен в процесс несколько раз.</exception>
    private Dictionary<string, Type> BuildRegistry()
    {
        var result = new Dictionary<string, Type>(StringComparer.Ordinal);

        foreach (var type in _typeSource.GetMessageTypes())
        {
            var discriminator = GetDiscriminator(type);

            if (!result.TryGetValue(discriminator, out var existingType))
            {
                result.Add(discriminator, type);
                continue;
            }

            if (existingType == type)
            {
                continue;
            }

            if (IsSameTypeIdentity(existingType, type))
            {
                throw new AmbiguousMessageTypeException(BuildAmbiguityMessage(discriminator, existingType, type));
            }

            throw new DiscriminatorConflictException(
                $"Discriminator '{discriminator}' is declared by several inbox message types: " +
                $"'{existingType.FullName}' and '{type.FullName}'. A discriminator must be unique.");
        }

        return result;
    }

    /// <summary>
    /// Один ли это логический тип, приехавший из разных контекстов загрузки.
    /// </summary>
    /// <remarks>
    /// Сравнение по <see cref="Type.AssemblyQualifiedName"/>, потому что ничем другим такие дубли не
    /// различить: имя типа, идентичность сборки и MVID у них совпадают, а сами <see cref="Type"/> при этом
    /// не равны. Разные версии сборки дают разный AQN и остаются честным конфликтом дискриминаторов.
    /// <para>
    /// Отсутствующий AQN (он есть только у незакрытых обобщений, которые дискавери и так отбрасывает)
    /// трактуется как разные типы: не сумев доказать, что тип один, честнее сообщить о конфликте.
    /// </para>
    /// </remarks>
    private static bool IsSameTypeIdentity(Type first, Type second) =>
        first.AssemblyQualifiedName is { } identity &&
        string.Equals(identity, second.AssemblyQualifiedName, StringComparison.Ordinal);

    /// <summary>
    /// Назвать настоящую причину и оба места: сборку грузят несколько раз, чинить надо хост.
    /// </summary>
    /// <remarks>
    /// Каждое вхождение описывается и контекстом загрузки, и путём к файлу. Различает копии именно контекст:
    /// в наблюдавшемся случае (тест-раннер Rider) обе копии лежали по ОДНОМУ пути и отличались только
    /// контекстом (Default против именованного по сборке). Путь добавлен потому, что он различается в других
    /// раскладках, когда сборку грузят из отдельной копии (shadow copy, часть инструментов покрытия), и одного
    /// контекста тогда мало. Вместе они опознают виновника при любой из причин.
    /// </remarks>
    private static string BuildAmbiguityMessage(string discriminator, Type first, Type second) =>
        $"Inbox message type '{first.AssemblyQualifiedName}' is loaded into this process more than once: " +
        $"{DescribeOccurrence(first)} and {DescribeOccurrence(second)}. " +
        $"Discriminator '{discriminator}' therefore maps to two different CLR types, and the inbox must not pick either: " +
        "handlers are registered for a single type identity, so picking the other one would silently leave these messages " +
        "unprocessed. Load the assembly once. A duplicate load usually comes from a test runner, coverage instrumentation " +
        "or a plugin host that loads the same assembly into its own load context.";

    /// <summary>
    /// Описать одно вхождение типа: чей контекст загрузки и из какого файла.
    /// </summary>
    private static string DescribeOccurrence(Type type)
    {
        var assembly = type.Assembly;
        var context = AssemblyLoadContext.GetLoadContext(assembly);
        var contextName = context is null ? "<unknown>" : context.Name ?? "<unnamed>";
        var location = string.IsNullOrEmpty(assembly.Location) ? "<in memory>" : assembly.Location;

        return $"[load context '{contextName}', assembly '{location}']";
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