using System;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Outbox.Options;

/// <summary>
/// Проверяет единственное правило: <see cref="OutboxOptions.MaxContentLengthBytes"/> положителен.
/// </summary>
/// <remarks>
/// Отдельный валидатор, а не <see cref="OutboxOptionsValidator"/> целиком: остальные правила аутбокса
/// исторически не исполнялись, и их включение отвергло бы конфигурации, которые сейчас работают (например
/// <see cref="OutboxOptions.GetFreeMessagesTimeout"/> ниже секунды), уронив существующие сервисы на старте
/// по причинам, не связанным с размером тела. Правило про размер введено вместе с самой опцией, поэтому
/// спящих потребителей у него нет и оно подключается сразу, симметрично инбоксу.
/// </remarks>
internal sealed class OutboxMaxContentLengthBytesValidator : IValidateOptions<OutboxOptions>
{
    public ValidateOptionsResult Validate(string? name, OutboxOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        // Тип в тексте обязателен: OptionsValidationException.Message это склейка отказов, имя типа опций
        // остаётся только в свойстве OptionsType. Инбокс объявляет одноимённую опцию, и без префикса лог
        // сервиса с обеими подсистемами не говорит, какую из них править.
        return options.MaxContentLengthBytes <= 0
            ? ValidateOptionsResult.Fail(
                $"{nameof(OutboxOptions)}.{nameof(OutboxOptions.MaxContentLengthBytes)} should be a positive number, " +
                $"but was {options.MaxContentLengthBytes}")
            : ValidateOptionsResult.Success;
    }
}