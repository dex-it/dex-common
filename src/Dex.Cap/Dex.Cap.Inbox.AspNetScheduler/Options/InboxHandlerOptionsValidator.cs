using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox.AspNetScheduler.Options;

internal sealed class InboxHandlerOptionsValidator : IValidateOptions<InboxHandlerOptions>
{
    private static readonly TimeSpan MaxInitDelay = TimeSpan.FromHours(1);

    /// <summary>
    /// Потолок интервала, который принимает <see cref="System.Threading.Tasks.Task.Delay(TimeSpan, System.Threading.CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// Ровно <c>uint.MaxValue - 1</c> миллисекунд (49.71 суток): это предел таймера платформы, и всё, что
    /// больше, роняет ожидание <see cref="ArgumentOutOfRangeException"/>. Граница взята техническая, а не
    /// «разумная»: любая более узкая была бы догадкой о намерениях потребителя и отвергала бы рабочие
    /// конфигурации, а эта отвергает ровно то, что всё равно не заработает.
    /// <para>
    /// Проверять обязательно здесь: все три величины уходят в <c>Task.Delay</c> уже ПОСЛЕ старта хоста
    /// (обработчик и чистильщик сначала выжидают свой InitDelay), поэтому без проверки хост поднимался бы
    /// «здоровым» и падал позже, в фоне, где причину пришлось бы искать по логам.
    /// </para>
    /// </remarks>
    private static readonly TimeSpan MaxDelay = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    public ValidateOptionsResult Validate(string? name, InboxHandlerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.Period <= TimeSpan.Zero)
            failures.Add($"{nameof(InboxHandlerOptions.Period)} should be positive, but was {options.Period}");

        if (options.CleanupInterval <= TimeSpan.Zero)
            failures.Add($"{nameof(InboxHandlerOptions.CleanupInterval)} should be positive, but was {options.CleanupInterval}");

        if (options.CleanupOlderThan <= TimeSpan.Zero)
            failures.Add($"{nameof(InboxHandlerOptions.CleanupOlderThan)} should be positive, but was {options.CleanupOlderThan}");

        if (options.CleanupBatchSize <= 0)
            failures.Add($"{nameof(InboxHandlerOptions.CleanupBatchSize)} should be positive, but was {options.CleanupBatchSize}");

        if (options.CleanupBatchDelay < TimeSpan.Zero)
            failures.Add($"{nameof(InboxHandlerOptions.CleanupBatchDelay)} should not be negative, but was {options.CleanupBatchDelay}");

        // Только те величины, что уходят в Task.Delay. CleanupOlderThan это ретеншен, а не ожидание:
        // 60 суток для него законное значение, и общий потолок отверг бы рабочую конфигурацию.
        CheckDelay(options.Period, nameof(InboxHandlerOptions.Period), failures);
        CheckDelay(options.CleanupInterval, nameof(InboxHandlerOptions.CleanupInterval), failures);
        CheckDelay(options.CleanupBatchDelay, nameof(InboxHandlerOptions.CleanupBatchDelay), failures);

        Check(options.HandlerInitDelay, nameof(InboxHandlerOptions.HandlerInitDelay), failures);
        Check(options.CleanerInitDelay, nameof(InboxHandlerOptions.CleanerInitDelay), failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void CheckDelay(TimeSpan value, string propertyName, List<string> failures)
    {
        if (value > MaxDelay)
            failures.Add($"{propertyName} should not exceed {MaxDelay} (the maximum delay the platform timer accepts), but was {value}");
    }

    private static void Check(InitDelayRange? range, string propertyName, List<string> failures)
    {
        if (range is null)
        {
            failures.Add($"{propertyName} should not be null");
            return;
        }

        if (range.Min < TimeSpan.Zero)
            failures.Add($"{propertyName}.{nameof(InitDelayRange.Min)} should not be negative, but was {range.Min}");

        if (range.Max < TimeSpan.Zero)
            failures.Add($"{propertyName}.{nameof(InitDelayRange.Max)} should not be negative, but was {range.Max}");

        if (range.Max > MaxInitDelay)
            failures.Add($"{propertyName}.{nameof(InitDelayRange.Max)} should not exceed {MaxInitDelay}, but was {range.Max}");

        if (range.Min > range.Max)
            failures.Add($"{propertyName}.{nameof(InitDelayRange.Min)} ({range.Min}) should not exceed {nameof(InitDelayRange.Max)} ({range.Max})");
    }
}