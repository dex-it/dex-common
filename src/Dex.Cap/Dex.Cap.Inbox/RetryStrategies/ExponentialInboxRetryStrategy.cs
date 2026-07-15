using System;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Options;

namespace Dex.Cap.Inbox.RetryStrategies;

/// <summary>
/// Повтор с экспоненциальным ростом задержки от номера попытки, с потолком.
/// </summary>
/// <remarks>
/// Задержка растёт как <c>baseDelay * 2^(retry-1)</c> и ограничена <c>maxDelay</c>.
/// Джиттер не добавляется: сообщения инбокса захватываются партиями через SKIP LOCKED,
/// поэтому синхронного «стада» повторов, ради которого нужен джиттер, здесь не возникает.
/// </remarks>
internal sealed class ExponentialInboxRetryStrategy(TimeSpan baseDelay, TimeSpan maxDelay) : IInboxRetryStrategy
{
    public DateTime CalculateNextStartDate(InboxRetryStrategyOptions? options)
    {
        var startDate = options?.StartDate ?? DateTime.UtcNow;
        var retry = options?.CurrentRetry ?? 1;

        if (retry < 1)
            retry = 1;

        // 2^(retry-1) считаем через double, чтобы не переполнить тики на большом числе попыток.
        var multiplier = Math.Pow(2, retry - 1);
        var delayTicks = baseDelay.Ticks * multiplier;

        var delay = delayTicks >= maxDelay.Ticks
            ? maxDelay
            : TimeSpan.FromTicks((long)delayTicks);

        return startDate.Add(delay);
    }
}
