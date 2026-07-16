using System;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Inbox.RetryStrategies;

/// <summary>
/// Выбор стратегии повторов. По умолчанию повторы идут без задержки.
/// </summary>
public sealed class InboxRetryStrategyConfigurator
{
    /// <summary>
    /// Верхняя граница задержки повтора.
    /// </summary>
    /// <remarks>
    /// Проверяется на входе, а не при вычислении StartAtUtc в фоновом обработчике: без неё абсурдное значение
    /// принималось бы публичным API как штатное, а падало позже и в другом месте, когда DateTime.Add выходит за
    /// DateTime.MaxValue и роняет фиксацию исхода. Тот же класс ошибки, что у аренды, где верхнюю границу уже
    /// поставили. Задержка больше года это заведомо ошибка конфигурации: такое сообщение проще похоронить.
    /// </remarks>
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromDays(365);

    private IInboxRetryStrategy _retryStrategy = new DefaultInboxRetryStrategy();

    /// <summary>
    /// Текущая стратегия повторов. По умолчанию повтор без задержки; присвоить null нельзя.
    /// </summary>
    public IInboxRetryStrategy RetryStrategy
    {
        get => _retryStrategy;
        set => _retryStrategy = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Повторять с фиксированным интервалом.
    /// </summary>
    public void UseIncrementalStrategy(TimeSpan interval)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(interval, MaxRetryDelay);
        RetryStrategy = new IncrementalInboxRetryStrategy(interval);
    }

    /// <summary>
    /// Повторять с экспоненциальной задержкой, ограниченной сверху.
    /// </summary>
    public void UseExponentialStrategy(TimeSpan baseDelay, TimeSpan maxDelay)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(baseDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDelay, baseDelay);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxDelay, MaxRetryDelay);
        RetryStrategy = new ExponentialInboxRetryStrategy(baseDelay, maxDelay);
    }
}