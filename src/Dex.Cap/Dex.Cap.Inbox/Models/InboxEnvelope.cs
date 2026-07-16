using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using JetBrains.Annotations;

// Non-nullable свойства заполняет EF через приватный конструктор при материализации из БД, поэтому компилятор
// их не видит заполненными. required здесь неприменим: он потребовал бы инициализатора и от EF, и от вызывающего
// кода, который заполняет сущность через публичный конструктор.
#pragma warning disable CS8618

namespace Dex.Cap.Inbox.Models;

/// <summary>
/// Входящее сообщение, сохранённое до подтверждения источнику.
/// </summary>
/// <remarks>
/// Дедупликация выполняется по паре <see cref="MessageId"/> + <see cref="ConsumerId"/>:
/// одно и то же сообщение может быть легитимно обработано разными потребителями,
/// поэтому идентификатора сообщения самого по себе недостаточно.
/// </remarks>
[Table(NameConst.TableName, Schema = NameConst.SchemaName)]
public class InboxEnvelope
{
    /// <summary>
    /// Максимальная длина <see cref="MessageId"/> и <see cref="ConsumerId"/>.
    /// </summary>
    /// <remarks>Ограничение нужно, потому что обе колонки входят в уникальный индекс.</remarks>
    public const int MaxIdentityLength = 256;

    /// <summary>
    /// Запас времени на фиксацию исхода, вычитаемый из аренды.
    /// </summary>
    /// <remarks>
    /// Обработка гасится на столько раньше окончания аренды в БД, чтобы успеть записать исход, пока аренда ещё
    /// наша: иначе фиксация не найдёт строку по ключу аренды и результат потерялся бы.
    /// </remarks>
    internal static readonly TimeSpan CompletionReserve = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Наименьшее окно, которое обязано остаться обработчику после вычета <see cref="CompletionReserve"/>.
    /// </summary>
    private static readonly TimeSpan MinProcessingWindow = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Минимально допустимая аренда.
    /// </summary>
    /// <remarks>
    /// Выводится из своих слагаемых, а не задаётся числом: минимум обязан совпадать с порогом, ниже которого
    /// задачу невозможно создать, иначе строка проходила бы отбор непригодных и роняла материализацию всей
    /// партии, то есть один ряд останавливал бы инбокс навсегда.
    /// </remarks>
    public static readonly TimeSpan MinLockTimeout = CompletionReserve + MinProcessingWindow;

    /// <summary>
    /// Максимально допустимая аренда.
    /// </summary>
    /// <remarks>
    /// Ограничение техническое: таймер отмены, которым гасится обработка, не принимает интервалы длиннее
    /// <see cref="int.MaxValue"/> миллисекунд (около 24.8 суток) и бросает. Без проверки такое значение
    /// принималось бы приёмом, а падало позже, при сборке задач, роняя материализацию ВСЕЙ захваченной
    /// партии: одно сообщение останавливало бы инбокс целиком.
    /// <para>
    /// Практического смысла в аренде такой длины нет: она означала бы, что упавший процесс держит свои
    /// сообщения заблокированными неделями.
    /// </para>
    /// </remarks>
    public static readonly TimeSpan MaxLockTimeout = TimeSpan.FromDays(1);

    /// <summary>
    /// Аренда по умолчанию.
    /// </summary>
    public static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromSeconds(30);

    [UsedImplicitly]
    private InboxEnvelope()
    {
    }

    public InboxEnvelope(Guid id, string messageId, string consumerId, string messageType, string content, TimeSpan? lockTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerId);
        ArgumentException.ThrowIfNullOrEmpty(messageType);
        ArgumentException.ThrowIfNullOrEmpty(content);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(messageId.Length, MaxIdentityLength);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(consumerId.Length, MaxIdentityLength);

        if (lockTimeout.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(lockTimeout.Value, MinLockTimeout);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(lockTimeout.Value, MaxLockTimeout);
        }

        Id = id;
        MessageId = messageId;
        ConsumerId = consumerId;
        MessageType = messageType;
        Content = content;
        Status = InboxMessageStatus.New;

        var now = DateTime.UtcNow;
        StartAtUtc = now;
        ScheduledStartIndexing = now;

        LockTimeout = lockTimeout ?? DefaultLockTimeout;
        ActivityId = Activity.Current?.Id;
    }

    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор сообщения в источнике: MessageId шины, Idempotency-Key HTTP-запроса и т.п.
    /// </summary>
    /// <remarks>Задаётся вызывающей стороной, потому что ядро не знает про транспорт.</remarks>
    [Required]
    [MaxLength(MaxIdentityLength)]
    public string MessageId { get; set; }

    /// <summary>
    /// Идентификатор потребителя, для которого принято сообщение.
    /// </summary>
    /// <remarks>Разделяет обработку одного сообщения разными потребителями в пределах одного хранилища.</remarks>
    [Required]
    [MaxLength(MaxIdentityLength)]
    public string ConsumerId { get; set; }

    /// <summary>
    /// Тип сообщения (дискриминатор).
    /// </summary>
    [Required]
    public string MessageType { get; set; }

    /// <summary>
    /// Сериализованное тело сообщения.
    /// </summary>
    /// <remarks>
    /// Библиотека не ограничивает длину тела сверху, в отличие от <see cref="MessageId"/> и
    /// <see cref="ConsumerId"/>, которым предел нужен из-за уникального индекса. Если источник недоверенный,
    /// размер тела ограничивают на транспорте (лимит брокера, максимальный размер HTTP-тела), а не здесь:
    /// подходящего универсального предела у библиотеки нет.
    /// </remarks>
    [Required]
    public string Content { get; set; }

    /// <summary>
    /// Идентификатор Activity.Id подсистемы System.Diagnostics.Activity.
    /// </summary>
    /// <remarks>Сохраняется при приёме, чтобы фоновая обработка продолжила трассу источника, а не начала свою.</remarks>
    public string? ActivityId { get; set; }

    /// <summary>
    /// Кол-во выполненных попыток обработки.
    /// </summary>
    public int Retries { get; set; }

    /// <summary>
    /// Статус сообщения.
    /// </summary>
    [Required]
    public InboxMessageStatus Status { get; set; }

    /// <summary>
    /// Сообщение об ошибке последней неудачной попытки.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Стэк ошибки последней неудачной попытки.
    /// </summary>
    public string? Error { get; set; }

    [Required]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? Updated { get; set; }

    /// <summary>
    /// Момент, начиная с которого сообщение можно обрабатывать (сдвигается ретрай-стратегией).
    /// </summary>
    public DateTime? StartAtUtc { get; set; }

    /// <summary>
    /// Копия <see cref="StartAtUtc"/> для частичного индекса выборки.
    /// </summary>
    /// <remarks>Сбрасывается в null у завершённых сообщений, чтобы вывести их из выборки и из индекса.</remarks>
    public DateTime? ScheduledStartIndexing { get; set; }

    #region Межпроцессная синхронизация

    /// <summary>
    /// Максимально допустимое время удержания блокировки.
    /// </summary>
    /// <remarks>
    /// Не меньше <see cref="MinLockTimeout"/> и обязан превышать время обработки ВСЕЙ захваченной партии, а не
    /// одного сообщения: аренда всех её сообщений тикает с момента захвата. Иначе аренда истечёт и сообщение
    /// будет обработано повторно.
    /// </remarks>
    [Required]
    public TimeSpan LockTimeout { get; set; }

    /// <summary>
    /// Уникальный ключ потока который захватил блокировку и только он имеет право её освободить (ключ идемпотентности).
    /// </summary>
    /// <remarks>Допускается игнорирование этого идентификатора если истекло время <see cref="LockExpirationTimeUtc"/>.</remarks>
    public Guid? LockId { get; set; }

    /// <summary>
    /// Максимально допустимый момент времени для удержания блокировки (превентивный тайм-аут).
    /// </summary>
    /// <remarks>
    /// Заменяет отдельный статус "в обработке": если процесс умер, аренда истечёт и сообщение вернётся в выборку,
    /// тогда как статус "в обработке" остался бы висеть навсегда.
    /// </remarks>
    public DateTime? LockExpirationTimeUtc { get; set; }

    #endregion
}
