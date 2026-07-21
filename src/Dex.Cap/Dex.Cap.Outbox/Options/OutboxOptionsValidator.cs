using System;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Outbox.Options;

/// <summary>
/// Правила <see cref="OutboxOptions"/>, кроме <see cref="OutboxOptions.MaxContentLength"/>.
/// </summary>
/// <remarks>
/// Валидатор исторически не подключён к контейнеру, поэтому перечисленные здесь правила не исполняются;
/// подключение целиком вынесено в issue #239. Размер тела намеренно вынесен в отдельный внутренний
/// <c>OutboxMaxContentLengthValidator</c>: его регистрирует <c>AddOutbox</c> из пакета
/// <c>Dex.Cap.Outbox.Ef</c>, поэтому именно там правило про размер работает на старте хоста. Дубль правила
/// здесь давал бы два сообщения об одной ошибке. Кто собирает DI руками, регистрирует правила сам.
/// </remarks>
public class OutboxOptionsValidator : IValidateOptions<OutboxOptions>
{
    public ValidateOptionsResult Validate(string? name, OutboxOptions? options)
    {
        if (options == null)
        {
            return ValidateOptionsResult.Fail("Configuration object is null.");
        }

        if (options.Retries is <= 0 or > 10_000)
        {
            return ValidateOptionsResult.Fail("Retries should be greater than 0 and less than 10000");
        }

        if (options.MessagesToProcess is <= 0 or > 100)
        {
            return ValidateOptionsResult.Fail("MessagesToProcess should be greater than 0 and less than 100");
        }

        if (options.ConcurrencyLimit is <= 0 or > 100)
        {
            return ValidateOptionsResult.Fail("ConcurrencyLimit should be greater than 0 and less than 100");
        }

        if (options.ConcurrencyLimit > options.MessagesToProcess)
        {
            return ValidateOptionsResult.Fail("ConcurrencyLimit can't be greater than MessagesToProcess");
        }

        if (options.GetFreeMessagesTimeout < TimeSpan.FromSeconds(1))
        {
            return ValidateOptionsResult.Fail("GetFreeMessagesTimeout can't be less 1 second");
        }

        return ValidateOptionsResult.Success;
    }
}