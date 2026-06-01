using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Outbox.AspNetScheduler.Options;

internal sealed class OutboxHandlerOptionsValidator : IValidateOptions<OutboxHandlerOptions>
{
    private static readonly TimeSpan MaxInitDelay = TimeSpan.FromHours(1);

    public ValidateOptionsResult Validate(string? name, OutboxHandlerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        if (options.Period <= TimeSpan.Zero)
            errors.Add($"{nameof(options.Period)} must be > 0 (got {options.Period})");

        if (options.CleanupInterval <= TimeSpan.Zero)
            errors.Add($"{nameof(options.CleanupInterval)} must be > 0 (got {options.CleanupInterval})");

        if (options.CleanupOlderThan <= TimeSpan.Zero)
            errors.Add($"{nameof(options.CleanupOlderThan)} must be > 0 (got {options.CleanupOlderThan})");

        Check(errors, nameof(options.HandlerInitDelay), options.HandlerInitDelay);
        Check(errors, nameof(options.CleanerInitDelay), options.CleanerInitDelay);

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static void Check(List<string> errors, string name, InitDelayRange? range)
    {
        if (range is null)
        {
            errors.Add($"{name} must not be null");
            return;
        }

        if (range.Min < TimeSpan.Zero)
        {
            errors.Add($"{name}.Min must be >= 0 (got {range.Min})");
        }

        if (range.Max < TimeSpan.Zero)
        {
            errors.Add($"{name}.Max must be >= 0 (got {range.Max})");
        }

        if (range.Max > MaxInitDelay)
        {
            errors.Add($"{name}.Max must be <= {MaxInitDelay} (got {range.Max})");
        }

        if (range.Min > range.Max)
        {
            errors.Add($"{name}.Min must be <= Max (got Min={range.Min}, Max={range.Max})");
        }
    }
}