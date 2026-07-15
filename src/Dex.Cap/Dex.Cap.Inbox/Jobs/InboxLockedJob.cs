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

    internal InboxLockedJob(InboxEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        Envelope = envelope;

        // Гасим обработку на 5 секунд раньше окончания аренды в БД, чтобы успеть зафиксировать исход,
        // пока аренда ещё наша: иначе Complete не найдёт строку по LockId и результат потеряется.
        Timeout = envelope.LockTimeout.Add(-TimeSpan.FromSeconds(5));

        _cts = new CancellationTokenSource();
        _cts.CancelAfter(Timeout);
        LockToken = _cts.Token;
    }

    public InboxEnvelope Envelope { get; }
    public Guid LockId => Envelope.LockId!.Value;
    public TimeSpan Timeout { get; }
    public CancellationToken LockToken { get; }

    public void Dispose()
    {
        _cts.Dispose();
    }
}
