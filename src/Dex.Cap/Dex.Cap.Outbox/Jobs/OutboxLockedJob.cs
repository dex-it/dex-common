using System;
using System.Threading;
using Dex.Cap.Outbox.Helpers;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox.Jobs
{
    /// <summary>
    /// Задача Outbox с захваченной пессимистичной блокировкой.
    /// </summary>
    internal sealed class OutboxLockedJob : IOutboxLockedJob
    {
        private CancellationTokenSource? _cts;
        private bool _disposed;

        internal OutboxLockedJob(OutboxEnvelope envelope, Guid lockId, TimeSpan timeout, CancellationTokenSource? cts)
        {
            _cts = cts;
            LockToken = cts?.Token ?? CancellationToken.None;
            Envelope = envelope;
            LockId = lockId;
            Timeout = timeout;
        }

        public OutboxEnvelope Envelope { get; }
        public Guid LockId { get; }
        public TimeSpan Timeout { get; }
        public CancellationToken LockToken { get; }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                NullableHelper.SetNull(ref _cts)?.Dispose();
            }
        }
    }
}