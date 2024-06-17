using System;
using System.Threading;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox.Jobs
{
    /// <summary>
    /// Задача Outbox с захваченной пессимистичной блокировкой.
    /// </summary>
    internal sealed class OutboxLockedJob : IOutboxLockedJob
    {
        private readonly CancellationTokenSource _cts;

        internal OutboxLockedJob(OutboxEnvelope envelope)
        {
            Envelope = envelope;
            Timeout = envelope.LockTimeout.Add(-TimeSpan.FromSeconds(5));

            if (Timeout < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(Timeout), "Timeout cannot be less than zero.");
            }

            if (Timeout < TimeSpan.FromSeconds(5))
            {
                throw new ArgumentOutOfRangeException(nameof(Timeout), "Timeout must be at least 5 seconds.");
            }

            _cts = new CancellationTokenSource();
            _cts.CancelAfter(Timeout);
            LockToken = _cts.Token;
        }

        public OutboxEnvelope Envelope { get; }
        public Guid LockId => Envelope.LockId!.Value;
        public TimeSpan Timeout { get; }
        public CancellationToken LockToken { get; }

        public void Dispose()
        {
            _cts.Dispose();
        }
    }
}