using System;
using System.Security.Cryptography;
using Dex.Cap.Inbox.Interfaces;
using Dex.Cap.Inbox.Options;

namespace Dex.Cap.Inbox.RetryStrategies;

/// <summary>
/// Повтор с экспоненциальным ростом задержки от номера попытки, с потолком и джиттером.
/// </summary>
/// <remarks>
/// Задержка растёт как <c>baseDelay * 2^(retry-1)</c>, ограничена <c>maxDelay</c> и отсчитывается от момента отказа.
/// <para>
/// Потолок достижим, только если <c>InboxOptions.Retries</c> достаточно велик: попытка, исчерпавшая лимит, хоронит
/// сообщение, не вычисляя задержку, поэтому при <c>Retries = N</c> множитель не превышает <c>2^(N-2)</c>.
/// При <c>Retries = 3</c> и <c>maxDelay</c> больше <c>2 * baseDelay</c> это <c>baseDelay</c> и <c>2 * baseDelay</c>,
/// а потолок недостижим. Конфигуратор разрешает и меньший <c>maxDelay</c>, вплоть до равного <c>baseDelay</c>,
/// и тогда потолок срабатывает раньше.
/// </para>
/// <para>
/// Задержка меньше <c>InboxHandlerOptions.Period</c> практически не наблюдаема: следующая попытка всё равно
/// случится не раньше очередного цикла обработчика.
/// </para>
/// </remarks>
internal sealed class ExponentialInboxRetryStrategy(TimeSpan baseDelay, TimeSpan maxDelay) : IInboxRetryStrategy
{
    /// <summary>Наибольшая доля задержки, на которую её укорачивает джиттер.</summary>
    private const double JitterRatio = 0.1;

    /// <summary>Число шагов дискретизации джиттера: генератор отдаёт целые, доля собирается из них.</summary>
    private const int JitterSteps = 1000;

    /// <inheritdoc />
    /// <remarks>
    /// Множитель 2^(retry-1) считается через double, чтобы не переполнить тики на большом числе попыток.
    /// </remarks>
    public DateTime CalculateNextStartDate(InboxRetryStrategyOptions options)
    {
        var retry = Math.Max(options.CurrentRetry, 1);

        var multiplier = Math.Pow(2, retry - 1);
        var delayTicks = baseDelay.Ticks * multiplier;

        var delay = delayTicks >= maxDelay.Ticks
            ? maxDelay
            : TimeSpan.FromTicks((long)delayTicks);

        return options.StartDate.Add(ApplyJitter(delay));
    }

    /// <summary>
    /// Развести моменты повтора сообщений, отказавших одновременно.
    /// </summary>
    /// <remarks>
    /// Массовый отказ (недоступен внешний сервис) роняет партию практически в один момент, поэтому без джиттера
    /// все её сообщения получают одинаковый StartAtUtc и становятся готовыми одновременно, давая
    /// восстанавливающемуся сервису самый резкий из возможных фронт нагрузки.
    /// <para>
    /// Инбокс страдает от «стада» слабее, чем независимые клиенты, ради которых джиттер вводили изначально: пиковая
    /// конкурентность здесь ограничена ConcurrencyLimit на реплику независимо от того, сколько сообщений созрело
    /// разом, то есть сам поллер работает ограничителем темпа. Но ограничен именно ПОТОЛОК конкурентности, а не
    /// одновременность старта, поэтому джиттер всё равно нужен: очереди на Postgres с SKIP LOCKED, где действует
    /// то же ограничение, добавляют его по умолчанию.
    /// </para>
    /// <para>
    /// Джиттер только укорачивает задержку, поэтому <c>maxDelay</c> остаётся жёстким потолком, а разброс
    /// сохраняется и на потолке, где сообщений скапливается больше всего. Симметричный джиттер потолок бы пробивал.
    /// </para>
    /// <para>
    /// Источник случайности криптографический не ради стойкости, а потому что это конвенция пакета: тем же
    /// генератором разводит свои старты <c>InitDelayRange</c>. Цена вызова несопоставима с задержкой, которую он
    /// вычисляет.
    /// </para>
    /// </remarks>
    private static TimeSpan ApplyJitter(TimeSpan delay)
    {
        var step = RandomNumberGenerator.GetInt32(0, JitterSteps + 1);
        var factor = 1 - JitterRatio * step / JitterSteps;

        return TimeSpan.FromTicks((long)(delay.Ticks * factor));
    }
}