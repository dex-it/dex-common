using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox.AspNetScheduler.Options;

internal sealed class InboxHandlerOptionsValidator : IValidateOptions<InboxHandlerOptions>
{
    private static readonly TimeSpan MaxInitDelay = TimeSpan.FromHours(1);

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

        Check(options.HandlerInitDelay, nameof(InboxHandlerOptions.HandlerInitDelay), failures);
        Check(options.CleanerInitDelay, nameof(InboxHandlerOptions.CleanerInitDelay), failures);

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
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
