using System;
using System.Threading;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox.Jobs;

/// <summary>
/// Задача инбокса с захваченной арендой.
/// </summary>
internal sealed class InboxLockedJob : IInboxLockedJob
{
    /// <summary>Минимальное окно обработки, остающееся после вычета запаса на фиксацию исхода.</summary>
    private static readonly TimeSpan MinTimeout = TimeSpan.FromSeconds(5);

    private readonly CancellationTokenSource _cts;

    internal InboxLockedJob(InboxEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        Envelope = envelope;

        // Гасим обработку на 5 секунд раньше окончания аренды в БД, чтобы успеть зафиксировать исход,
        // пока аренда ещё наша: иначе Complete не найдёт строку по LockId и результат потеряется.
        Timeout = envelope.LockTimeout.Add(-TimeSpan.FromSeconds(5));

        // Конструктор InboxEnvelope требует LockTimeout не меньше 10 секунд, но сюда конверт приезжает
        // из БД через приватный конструктор EF, минуя эту проверку. Строка с некорректным LockTimeout
        // (правка руками, чужой писатель, изменённый дефолт колонки) дала бы отрицательный CancelAfter,
        // а он бросает. Это уронило бы материализацию ВСЕЙ партии в GetWaitingJobs, то есть один битый
        // ряд остановил бы весь инбокс навсегда, отдавая наружу только LogCritical раз в Period.
        if (Timeout < MinTimeout)
        {
            throw new ArgumentOutOfRangeException(
                nameof(envelope),
                envelope.LockTimeout,
                $"Inbox message {envelope.Id} has a LockTimeout below the required minimum of {InboxEnvelope.MinLockTimeout}.");
        }

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
