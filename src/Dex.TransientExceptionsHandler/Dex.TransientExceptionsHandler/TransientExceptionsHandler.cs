using System.Collections.Frozen;

namespace Dex.TransientExceptionsHandler;

/// <summary>
/// Позволяет настроить перехват временных ошибок в UseRetryConfiguration и UseRedeliveryRetryConfiguration
/// </summary>
public partial class TransientExceptionsHandler
{
    private readonly int _innerExceptionsSearchDepth;

    private HashSet<Type>? _transientExceptionsConfig = [];
    private Dictionary<Type, Func<Exception, bool>>? _transientExceptionsPredicateConfig = new();

    private FrozenSet<Type>? _transientExceptions;
    private FrozenDictionary<Type, Func<Exception, bool>>? _transientExceptionsPredicate;

    /// <summary>
    /// False - настройка не завершена: можно делать только Add
    /// <br/>
    /// True - настройка завершена: можно делать только Check
    /// </summary>
    public bool IsFrozen { get; private set; }

    public TransientExceptionsHandler(IEnumerable<Type>? exceptionTypes = null, int? innerExceptionsSearchDepth = null, bool toSeal = false)
    {
        _innerExceptionsSearchDepth = innerExceptionsSearchDepth is > 0
            ? innerExceptionsSearchDepth.Value
            : DefaultInnerExceptionSearchDepth;

        if (exceptionTypes is not null) Add(exceptionTypes);
        if (toSeal) ToSeal();
    }

    /// <summary>
    /// Указываем дополнительные исключения, которые будем считать трансиентными
    /// </summary>
    public TransientExceptionsHandler Add(IEnumerable<Type> exceptionTypes)
    {
        foreach (var exceptionType in exceptionTypes) Add(exceptionType);
        return this;
    }

    /// <summary>
    /// Указываем дополнительные исключения, которые будем считать трансиентными
    /// </summary>
    public TransientExceptionsHandler Add(Type exceptionType)
    {
        if (IsFrozen) throw new InvalidOperationException($"{nameof(TransientExceptionsHandler)} is frozen.");

        if (exceptionType.IsSubclassOf(typeof(Exception)) || exceptionType == typeof(Exception))
            _transientExceptionsConfig!.Add(exceptionType);
        else
            throw new ArgumentException($"Type {exceptionType} is not a valid exception type.");

        return this;
    }

    /// <summary>
    /// Указываем дополнительные делегаты, которые будем считать трансиентными
    /// </summary>
    public TransientExceptionsHandler Add<T>(Func<T, bool> predicate) where T : Exception
    {
        if (IsFrozen) throw new InvalidOperationException($"{nameof(TransientExceptionsHandler)} is frozen.");

        _transientExceptionsPredicateConfig!.Add(typeof(T), (Func<Exception, bool>)predicate);
        return this;
    }

    /// <summary>
    /// Проверяем, является ли ошибка трансиентной
    /// </summary>
    public bool Check(Exception exception)
    {
        if (IsFrozen is false)
            throw new InvalidOperationException(
                $"Завершите настройку {nameof(TransientExceptionsHandler)} и вызовите {nameof(ToSeal)} перед использованием {nameof(Check)}");

        if (ExceptionsCheckInternal(_transientExceptions!, exception, _innerExceptionsSearchDepth)) return true;
        if (PredicateCheckInternal(_transientExceptionsPredicate!, exception, _innerExceptionsSearchDepth)) return true;

        return StaticCheck(exception, _innerExceptionsSearchDepth);
    }

    public void ToSeal()
    {
        IsFrozen = true;

        _transientExceptions = _transientExceptionsConfig!.ToFrozenSet();
        _transientExceptionsPredicate = _transientExceptionsPredicateConfig!.ToFrozenDictionary();

        _transientExceptionsConfig = null;
        _transientExceptionsPredicateConfig = null;
    }
}