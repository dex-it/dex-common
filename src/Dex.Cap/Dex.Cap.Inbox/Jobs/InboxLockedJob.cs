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
    /// Аренда конверта вне диапазона [<see cref="InboxEnvelope.MinLockTimeout"/>,
    /// <see cref="InboxEnvelope.MaxLockTimeout"/>]. Конструктор <see cref="InboxEnvelope"/> этого не допускает, но
    /// сюда конверт приезжает из БД через приватный конструктор EF, минуя проверку. Слишком короткая аренда дала бы
    /// отрицательный CancelAfter, слишком длинная не влезла бы в таймер, и оба случая бросают: это уронило бы
    /// материализацию ВСЕЙ партии, то есть один битый ряд остановил бы весь инбокс навсегда, отдавая наружу только
    /// LogCritical раз в цикл. Отбор непригодных строк идёт по тем же границам и до сборки задач, поэтому сюда
    /// такой конверт не доходит.
    /// </exception>
    internal InboxLockedJob(InboxEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.LockTimeout < InboxEnvelope.MinLockTimeout || envelope.LockTimeout > InboxEnvelope.MaxLockTimeout)
        {
            throw new ArgumentOutOfRangeException(
                nameof(envelope),
                envelope.LockTimeout,
                $"Inbox message {envelope.Id} has a LockTimeout outside the allowed range " +
                $"[{InboxEnvelope.MinLockTimeout}, {InboxEnvelope.MaxLockTimeout}].");
        }

        Envelope = envelope;

        _cts = new CancellationTokenSource();
        _cts.CancelAfter(envelope.LockTimeout.Subtract(InboxEnvelope.CompletionReserve));
        LockToken = _cts.Token;
    }

    public InboxEnvelope Envelope { get; }
    public Guid LockId => Envelope.LockId!.Value;
    public CancellationToken LockToken { get; }

    public void Dispose()
    {
        _cts.Dispose();
    }
}