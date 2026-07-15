using System;
using System.Threading;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox.Jobs;

/// <summary>
/// Задача инбокса с захваченной арендой.
/// </summary>
internal sealed class InboxLockedJob : IInboxLockedJob
{
    private readonly CancellationTokenSource _cts;

    /// <summary>
    /// Взять сообщение в работу, взведя таймер аренды.
    /// </summary>
    /// <remarks>
    /// Обработка гасится на <see cref="InboxEnvelope.CompletionReserve"/> раньше окончания аренды в БД, чтобы
    /// успеть зафиксировать исход, пока аренда ещё наша.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Аренда конверта короче <see cref="InboxEnvelope.MinLockTimeout"/>. Конструктор <see cref="InboxEnvelope"/>
    /// этого не допускает, но сюда конверт приезжает из БД через приватный конструктор EF, минуя проверку. Строка
    /// с некорректным LockTimeout (правка руками, чужой писатель, изменённый дефолт колонки) дала бы отрицательный
    /// CancelAfter, а он бросает: это уронило бы материализацию ВСЕЙ партии, то есть один битый ряд остановил бы
    /// весь инбокс навсегда, отдавая наружу только LogCritical раз в цикл. Отбор непригодных строк идёт по той же
    /// величине и до сборки задач, поэтому сюда такой конверт не доходит.
    /// </exception>
    internal InboxLockedJob(InboxEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.LockTimeout < InboxEnvelope.MinLockTimeout)
        {
            throw new ArgumentOutOfRangeException(
                nameof(envelope),
                envelope.LockTimeout,
                $"Inbox message {envelope.Id} has a LockTimeout below the required minimum of {InboxEnvelope.MinLockTimeout}.");
        }

        Envelope = envelope;
        Timeout = envelope.LockTimeout.Subtract(InboxEnvelope.CompletionReserve);

        _cts = new CancellationTokenSource();
        _cts.CancelAfter(Timeout);
        LockToken = _cts.Token;
    }

    public InboxEnvelope Envelope { get; }
    public Guid LockId => Envelope.LockId!.Value;
    public CancellationToken LockToken { get; }

    /// <summary>Окно, остающееся обработчику после вычета запаса на фиксацию исхода.</summary>
    private TimeSpan Timeout { get; }

    public void Dispose()
    {
        _cts.Dispose();
    }
}
