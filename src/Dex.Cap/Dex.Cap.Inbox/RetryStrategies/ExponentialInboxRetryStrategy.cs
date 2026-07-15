using System;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Options;

namespace Dex.Cap.Inbox.RetryStrategies;

/// <summary>
/// Повтор с экспоненциальным ростом задержки от номера попытки, с потолком.
/// </summary>
/// <remarks>
/// Задержка растёт как <c>baseDelay * 2^(retry-1)</c>, ограничена <c>maxDelay</c> и отсчитывается от момента отказа.
/// <para>
/// Потолок достижим, только если <c>InboxOptions.Retries</c> достаточно велик: попытка, исчерпавшая лимит, хоронит
/// сообщение, не вычисляя задержку, поэтому при <c>Retries = N</c> множитель не превышает <c>2^(N-2)</c>.
/// При <c>Retries = 3</c> это <c>baseDelay</c> и <c>2*baseDelay</c>, а <c>maxDelay</c> недостижим.
/// </para>
/// <para>
/// Задержка меньше <c>InboxHandlerOptions.Period</c> практически не наблюдаема: следующая попытка всё равно
/// случится не раньше очередного цикла обработчика.
/// </para>
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
