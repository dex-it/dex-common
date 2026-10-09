using System.Collections.Frozen;
using Dex.TransientExceptions.Exceptions;

namespace Dex.TransientExceptions;

/// <summary>
/// Позволяет настроить перехват временных ошибок в UseRetryConfiguration и UseRedeliveryRetryConfiguration
/// </summary>
public partial class TransientExceptionsHandler
{
    private bool _disableDefaultBehaviour;
    private int _innerExceptionsSearchDepth;

    private HashSet<Type>? _transientExceptionsPreBuildConfig = [];
    private Dictionary<Type, Func<Exception, bool>>? _transientExceptionsPredicatePreBuildConfig = new();

    private FrozenSet<Type>? _transientExceptions;
    private FrozenDictionary<Type, Func<Exception, bool>>? _transientExceptionsPredicate;

    /// <summary>
    /// False - настройка не завершена: можно делать только Add
    /// <br/>
    /// True - настройка завершена: можно делать только Check
    /// </summary>
    public bool BuildCompleted { get; private set; }

    public TransientExceptionsHandler(
        IEnumerable<Type>? exceptionTypes = null,
        int? innerExceptionsSearchDepth = null,
        bool runBuild = false,
        bool disableDefaultBehaviour = false)
    {
        _disableDefaultBehaviour = disableDefaultBehaviour;

        if (innerExceptionsSearchDepth is { } depth)
            ArgumentOutOfRangeException.ThrowIfNegative(depth, nameof(innerExceptionsSearchDepth));

        _innerExceptionsSearchDepth = innerExceptionsSearchDepth ?? DefaultInnerExceptionSearchDepth;

        if (exceptionTypes is not null)
            Add(exceptionTypes);

        if (runBuild)
            Build();
    }

    /// <summary>
    /// Указываем дополнительные исключения, которые будем считать трансиентными
    /// </summary>
    public TransientExceptionsHandler Add(IEnumerable<Type> exceptionTypes)
    {
        ArgumentNullException.ThrowIfNull(exceptionTypes);

        foreach (var exceptionType in exceptionTypes)
            Add(exceptionType);

        return this;
    }

    /// <summary>
    /// Указываем дополнительные исключения, которые будем считать трансиентными
    /// </summary>
    public TransientExceptionsHandler Add(Type exceptionType)
    {
        ArgumentNullException.ThrowIfNull(exceptionType);

        if (BuildCompleted)
            throw new InvalidOperationException($"{nameof(TransientExceptionsHandler)} is already built");

        if (exceptionType.IsSubclassOf(typeof(Exception)) || exceptionType == typeof(Exception))
            _transientExceptionsPreBuildConfig!.Add(exceptionType);
        else
            throw new ArgumentException($"Type {exceptionType} is not a valid exception type.");

        return this;
    }

    /// <summary>
    /// Указываем дополнительные делегаты, которые будем считать трансиентными
    /// </summary>
    public TransientExceptionsHandler Add<T>(Func<T, bool> predicate) where T : Exception
    {
        if (BuildCompleted)
            throw new InvalidOperationException($"{nameof(TransientExceptionsHandler)} is already built");

        _transientExceptionsPredicatePreBuildConfig!.Add(typeof(T), ex => predicate((T)ex));

        return this;
    }

    /// <summary>
    /// Проверяем, является ли ошибка трансиентной
    /// </summary>
    public bool Check(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (BuildCompleted is false)
            throw new InvalidOperationException(
                $"Завершите настройку {nameof(TransientExceptionsHandler)} и вызовите {nameof(Build)} перед использованием {nameof(Check)}");

        return MarkerCheck(exception, _innerExceptionsSearchDepth) ?? TypeCheck(exception, _innerExceptionsSearchDepth);
    }

    // null = маркеров нет, решают типы по всему дереву
    // true/false = первый маркер на пути вниз по InnerException; его решение финальное и для типов выше него
    private bool? MarkerCheck(Exception exception, int innerExceptionsSearchDepth)
    {
        for (var (current, level) = (exception, 1); ; level++)
        {
            // ITransientExceptionCandidate проверяется первым — более специфичный контракт,
            // позволяет явно переопределить поведение даже если базовый класс реализует ITransientException
            if (current is ITransientExceptionCandidate candidate)
                return candidate.IsTransient;

            if (current is ITransientException)
                return true;

            if (level >= innerExceptionsSearchDepth)
                return null;

            if (current is AggregateException aggregate)
                return AggregateMarkerCheck(aggregate, innerExceptionsSearchDepth - level);

            if (current.InnerException is null)
                return null;

            current = current.InnerException;
        }
    }

    // Ветки агрегата между собой не вложены, поэтому маркер одной ветки не решает за соседнюю:
    // каждая проверяется целиком, transient — если transient хоть одна
    private bool? AggregateMarkerCheck(AggregateException aggregate, int branchSearchDepth)
    {
        var branches = aggregate.InnerExceptions;
        var markers = new bool?[branches.Count];
        var markerFound = false;

        for (var i = 0; i < branches.Count; i++)
        {
            markers[i] = MarkerCheck(branches[i], branchSearchDepth);
            if (markers[i] is true)
                return true;

            markerFound |= markers[i].HasValue;
        }

        if (!markerFound)
            return null;

        for (var i = 0; i < branches.Count; i++)
            if (markers[i] is null && TypeCheck(branches[i], branchSearchDepth))
                return true;

        return false;
    }

    private bool TypeCheck(Exception exception, int innerExceptionsSearchDepth)
    {
        if (ExceptionsCheckInternal(_transientExceptions!, exception, innerExceptionsSearchDepth))
            return true;

        if (PredicateCheckInternal(_transientExceptionsPredicate!, exception, innerExceptionsSearchDepth))
            return true;

        // run default behaviour if it is not disabled
        return !_disableDefaultBehaviour && StaticCheck(exception, innerExceptionsSearchDepth);
    }

    /// <summary>
    /// Отключить стандартные проверки трансиентности
    /// </summary>
    public TransientExceptionsHandler DisableDefaultBehaviour()
    {
        if (BuildCompleted)
            throw new InvalidOperationException($"{nameof(TransientExceptionsHandler)} is already built");

        _disableDefaultBehaviour = true;

        return this;
    }

    public TransientExceptionsHandler SetInnerExceptionsSearchDepth(int depth)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(depth);

        if (BuildCompleted)
            throw new InvalidOperationException($"{nameof(TransientExceptionsHandler)} is already built");

        _innerExceptionsSearchDepth = depth;

        return this;
    }

    public TransientExceptionsHandler Build()
    {
        if (BuildCompleted)
            throw new InvalidOperationException($"{nameof(TransientExceptionsHandler)} is already built");

        BuildCompleted = true;

        _transientExceptions = _transientExceptionsPreBuildConfig!.ToFrozenSet();
        _transientExceptionsPredicate = _transientExceptionsPredicatePreBuildConfig!.ToFrozenDictionary();

        _transientExceptionsPreBuildConfig = null;
        _transientExceptionsPredicatePreBuildConfig = null;

        return this;
    }
}