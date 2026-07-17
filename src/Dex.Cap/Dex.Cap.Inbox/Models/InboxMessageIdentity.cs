using System;

namespace Dex.Cap.Inbox.Models;

/// <summary>
/// Идентичность входящего сообщения: ключ дедупликации.
/// </summary>
/// <remarks>
/// Ядро не знает про транспорт, поэтому оба значения задаёт вызывающая сторона.
/// </remarks>
public readonly record struct InboxMessageIdentity
{
    /// <summary>
    /// Задать идентичность сообщения парой «идентификатор сообщения - потребитель».
    /// </summary>
    public InboxMessageIdentity(string messageId, string consumerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerId);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(messageId.Length, InboxEnvelope.MaxIdentityLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(consumerId.Length, InboxEnvelope.MaxIdentityLength);

        MessageId = messageId;
        ConsumerId = consumerId;
    }

    /// <summary>
    /// Идентификатор сообщения в источнике: для шины — идентификатор сообщения брокера,
    /// для HTTP — значение Idempotency-Key.
    /// </summary>
    /// <remarks>
    /// Обязан быть стабилен между повторными доставками одного и того же сообщения,
    /// иначе дедупликация не работает: каждая доставка будет принята как новая.
    /// </remarks>
    public string MessageId { get; }

    /// <summary>
    /// Идентификатор потребителя: имя эндпоинта, тип консьюмера, маршрут HTTP.
    /// </summary>
    /// <remarks>
    /// Обязан быть стабилен между перезапусками и одинаков на всех инстансах сервиса,
    /// поэтому значения, меняющиеся от инстанса к инстансу, использовать нельзя.
    /// </remarks>
    public string ConsumerId { get; }

    /// <summary>
    /// Отвергнуть неинициализированную идентичность.
    /// </summary>
    /// <remarks>
    /// Проверки конструктора обходит <c>default(InboxMessageIdentity)</c>, и запретить это нельзя: значение по
    /// умолчанию есть у любой структуры, а конструктор для него не вызывается. Значит, единственное место, где
    /// такую пару можно отвергнуть, это граница, через которую она входит внутрь.
    /// <para>
    /// Без проверки обе половины ядра врут по-разному, и обе молча. Приём дошёл бы до конструктора конверта и
    /// упал там с именем ВНУТРЕННЕГО параметра ('messageId'), которого вызывающий не передавал: он передавал
    /// identity. А возврат из dead letter не упал бы вовсе: предикат сравнялся бы с null, не нашёл бы ни одной
    /// строки и вернул бы штатное «возвращать нечего», неотличимое от «такого сообщения нет».
    /// </para>
    /// </remarks>
    /// <param name="paramName">Имя параметра вызывающего метода: в исключении обязано стоять оно, а не наше.</param>
    /// <exception cref="ArgumentException">Идентичность не создавалась конструктором.</exception>
    internal void EnsureInitialized(string paramName)
    {
        if (MessageId is not null && ConsumerId is not null)
        {
            return;
        }

        throw new ArgumentException(
            $"{nameof(InboxMessageIdentity)} is not initialized: a default value carries neither " +
            $"{nameof(MessageId)} nor {nameof(ConsumerId)}. Build it with its constructor.",
            paramName);
    }
}