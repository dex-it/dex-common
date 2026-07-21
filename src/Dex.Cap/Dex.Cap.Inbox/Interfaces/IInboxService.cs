using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox.Interfaces;

/// <summary>
/// Приём входящих сообщений: точка входа инбокса для транспорта.
/// </summary>
public interface IInboxService
{
    /// <summary>
    /// Принять входящее сообщение и сохранить его для последующей фоновой обработки.
    /// </summary>
    /// <remarks>
    /// В отличие от <c>IOutboxService.EnqueueAsync</c> метод сохраняет сообщение немедленно, своей транзакцией:
    /// смысл инбокса в том, чтобы зафиксировать сообщение ДО подтверждения источнику, а бизнес-работа
    /// выполняется позже фоновым обработчиком.
    /// <para>
    /// Поэтому вызывать метод внутри транзакции вызывающего кода нельзя: на откате сообщение исчезло бы,
    /// хотя источнику уже отправлено подтверждение. Открытая транзакция хранилища или окружающий
    /// <see cref="System.Transactions.TransactionScope"/> приводят к <see cref="Exceptions.InboxException"/>.
    /// </para>
    /// <para>
    /// Повторная доставка того же сообщения возвращает <see cref="InboxEnqueueStatus.Duplicate"/> без исключения:
    /// источнику в этом случае следует подтвердить сообщение, а не отправлять его в очередь ошибок.
    /// Единственное исключение из этого правила: размер тела проверяется ДО вставки, а дедупликация происходит
    /// В ней, поэтому передоставка тела, которое уже лежит в таблице, но перестало проходить
    /// <see cref="Options.InboxOptions.MaxContentLengthBytes"/> (предел понизили или подняли версию), отвергается
    /// <see cref="Exceptions.InboxContentTooLargeException"/>. Отказ детерминирован, так что такое сообщение
    /// уводят в очередь ошибок, а не передоставляют снова.
    /// </para>
    /// <para>
    /// Транзиентный отказ хранилища доходит до вызывающего исключением, и настроенный на контексте
    /// EnableRetryOnFailure этого НЕ меняет: вставка идёт через <c>ExecuteSqlRawAsync</c>, а он стратегию
    /// повторов не использует by design. Так и задумано: подтверждать источнику нечего, пока сообщение не
    /// сохранено, поэтому отказ обязан быть виден, а роль повтора играет редоставка источника. Отсюда же
    /// следует, что вызывающий, повторивший приём сам, увидит <see cref="InboxEnqueueStatus.Duplicate"/> на
    /// строку, вставленную его же предыдущей попыткой.
    /// </para>
    /// NOTE. lockTimeout должен превышать время обработки ВСЕЙ захваченной партии, а не одного сообщения:
    /// аренда всех сообщений партии тикает с момента захвата. Иначе аренда истечёт и сообщение будет
    /// обработано повторно. По умолчанию <see cref="Models.InboxEnvelope.DefaultLockTimeout"/>, диапазон от
    /// <see cref="Models.InboxEnvelope.MinLockTimeout"/> до <see cref="Models.InboxEnvelope.MaxLockTimeout"/>.
    /// </remarks>
    /// <exception cref="Exceptions.InboxException">
    /// Приём выполняется внутри транзакции вызывающего кода.
    /// </exception>
    /// <exception cref="Exceptions.DiscriminatorResolveException">
    /// Тип сообщения не найден среди загруженных типов сервиса. Наследует <see cref="Exceptions.InboxException"/>.
    /// </exception>
    /// <exception cref="Exceptions.InboxContentTooLargeException">
    /// Размер сериализованного тела превышает <see cref="Options.InboxOptions.MaxContentLengthBytes"/>.
    /// Наследует <see cref="Exceptions.InboxException"/>.
    /// </exception>
    Task<InboxEnqueueStatus> EnqueueAsync<T>(
        T message,
        InboxMessageIdentity identity,
        TimeSpan? lockTimeout = null,
        CancellationToken cancellationToken = default)
        where T : class, IInboxMessage;
}