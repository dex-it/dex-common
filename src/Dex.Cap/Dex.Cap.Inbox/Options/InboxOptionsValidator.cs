using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox.Options;

internal sealed class InboxOptionsValidator : IValidateOptions<InboxOptions>
{
    public ValidateOptionsResult Validate(string? name, InboxOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.Retries <= 0)
            failures.Add($"{nameof(InboxOptions.Retries)} should be a positive number, but was {options.Retries}");

        if (options.MessagesToProcess <= 0)
            failures.Add($"{nameof(InboxOptions.MessagesToProcess)} should be a positive number, but was {options.MessagesToProcess}");

        if (options.ConcurrencyLimit <= 0)
            failures.Add($"{nameof(InboxOptions.ConcurrencyLimit)} should be a positive number, but was {options.ConcurrencyLimit}");

        if (options.ConcurrencyLimit > options.MessagesToProcess)
        {
            failures.Add(
                $"{nameof(InboxOptions.ConcurrencyLimit)} ({options.ConcurrencyLimit}) should not exceed " +
                $"{nameof(InboxOptions.MessagesToProcess)} ({options.MessagesToProcess}): extra parallelism has nothing to process");
        }

        if (options.GetFreeMessagesTimeout <= TimeSpan.Zero)
            failures.Add($"{nameof(InboxOptions.GetFreeMessagesTimeout)} should be positive, but was {options.GetFreeMessagesTimeout}");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
