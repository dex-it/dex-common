using System;
using System.Threading;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox.Jobs;

/// <summary>
/// Задача инбокса с захваченной арендой.
/// </summary>
internal sealed class InboxLockedJob : IInboxLockedJob
{
    /// <summary>Запас времени на фиксацию исхода, вычитаемый из аренды.</summary>
    private static readonly TimeSpan CompletionReserve = TimeSpan.FromSeconds(5);

    /// <summary>Минимальное окно обработки, остающееся после вычета запаса на фиксацию исхода.</summary>
    private static readonly TimeSpan MinTimeout = TimeSpan.FromSeconds(5);

    private readonly CancellationTokenSource _cts;

    /// <summary>
    /// Взять сообщение в работу, взведя таймер аренды.
    /// </summary>
    /// <remarks>
    /// Обработка гасится на <see cref="CompletionReserve"/> раньше окончания аренды в БД, чтобы успеть
    /// зафиксировать исход, пока аренда ещё наша: иначе фиксация не найдёт строку по ключу аренды и
    /// результат потеряется.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Аренда конверта короче минимума. Конструктор <see cref="InboxEnvelope"/> этого не допускает, но
    /// сюда конверт приезжает из БД через приватный конструктор EF, минуя проверку. Строка с некорректным
    /// LockTimeout (правка руками, чужой писатель, изменённый дефолт колонки) дала бы отрицательный
    /// CancelAfter, а он бросает: это уронило бы материализацию ВСЕЙ партии, то есть один битый ряд
    /// остановил бы весь инбокс навсегда, отдавая наружу только LogCritical раз в цикл.
    /// </exception>
    internal InboxLockedJob(InboxEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        Envelope = envelope;
        Timeout = envelope.LockTimeout.Subtract(CompletionReserve);

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
