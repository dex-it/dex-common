using System.Collections.Frozen;

namespace Dex.TransientExceptions;

/// <summary>
/// Позволяет настроить перехват временных ошибок в UseRetryConfiguration и UseRedeliveryRetryConfiguration
/// </summary>
public partial class TransientExceptionsHandler
{
    private readonly bool _disableDefaultBehaviour;
    private readonly int _innerExceptionsSearchDepth;

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

        _innerExceptionsSearchDepth = innerExceptionsSearchDepth is > 0
            ? innerExceptionsSearchDepth.Value
            : DefaultInnerExceptionSearchDepth;

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

        _transientExceptionsPredicatePreBuildConfig!.Add(typeof(T), (Func<Exception, bool>)predicate);

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

        if (ExceptionsCheckInternal(_transientExceptions!, exception, _innerExceptionsSearchDepth))
            return true;

        if (PredicateCheckInternal(_transientExceptionsPredicate!, exception, _innerExceptionsSearchDepth))
            return true;

        // run default behaviour if it is not disabled
        return !_disableDefaultBehaviour && StaticCheck(exception, _innerExceptionsSearchDepth);
    }

    public void Build()
    {
        if (BuildCompleted)
            throw new InvalidOperationException($"{nameof(TransientExceptionsHandler)} is already built");

        BuildCompleted = true;

        _transientExceptions = _transientExceptionsPreBuildConfig!.ToFrozenSet();
        _transientExceptionsPredicate = _transientExceptionsPredicatePreBuildConfig!.ToFrozenDictionary();

        _transientExceptionsPreBuildConfig = null;
        _transientExceptionsPredicatePreBuildConfig = null;
    }
}