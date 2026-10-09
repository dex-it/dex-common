using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Inbox.Options;

internal sealed class InboxOptionsValidator : IValidateOptions<InboxOptions>
{
    /// <summary>Наименьший таймаут захвата, переживающий перевод в целые секунды.</summary>
    private static readonly TimeSpan MinGetFreeMessagesTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Имя типа опций, которым префиксуются все сообщения об отказе.</summary>
    private const string Owner = nameof(InboxOptions);

    public ValidateOptionsResult Validate(string? name, InboxOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Тип в тексте обязателен у КАЖДОГО правила: аутбокс объявляет опции с теми же именами, а
        // OptionsValidationException.Message это склейка отказов, имя типа опций остаётся только в свойстве
        // OptionsType. Без префикса лог сервиса с обеими подсистемами не говорит, какую из них править.
        if (options.Retries <= 0)
            failures.Add($"{Owner}.{nameof(InboxOptions.Retries)} should be a positive number, but was {options.Retries}");

        if (options.MessagesToProcess <= 0)
            failures.Add($"{Owner}.{nameof(InboxOptions.MessagesToProcess)} should be a positive number, but was {options.MessagesToProcess}");

        if (options.ConcurrencyLimit <= 0)
            failures.Add($"{Owner}.{nameof(InboxOptions.ConcurrencyLimit)} should be a positive number, but was {options.ConcurrencyLimit}");

        if (options.ConcurrencyLimit > options.MessagesToProcess)
        {
            failures.Add(
                $"{Owner}.{nameof(InboxOptions.ConcurrencyLimit)} ({options.ConcurrencyLimit}) should not exceed " +
                $"{nameof(InboxOptions.MessagesToProcess)} ({options.MessagesToProcess}): extra parallelism has nothing to process");
        }

        if (options.GetFreeMessagesTimeout < MinGetFreeMessagesTimeout)
        {
            failures.Add(
                $"{Owner}.{nameof(InboxOptions.GetFreeMessagesTimeout)} should be at least {MinGetFreeMessagesTimeout}, " +
                $"but was {options.GetFreeMessagesTimeout}: the command timeout is expressed in whole seconds, " +
                "so a smaller value truncates to zero and silently leaves the timeout unset");
        }

        if (options.MaxContentLengthBytes <= 0)
        {
            failures.Add(
                $"{Owner}.{nameof(InboxOptions.MaxContentLengthBytes)} should be a positive number, " +
                $"but was {options.MaxContentLengthBytes}");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}