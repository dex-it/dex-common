using System;
using System.Threading;
using Dex.Cap.Outbox.Helpers;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox.Jobs
{
    /// <summary>
    /// Задача Outbox с захваченной пессимистичной блокировкой.
    /// </summary>
    internal sealed class OutboxLockedJob : IOutboxLockedJob, IDisposable
    {
        private readonly CancellationToken _cancellationToken;
        private CancellationTokenSource? _cts;
        private bool _disposed;

        internal OutboxLockedJob(OutboxEnvelope envelope, Guid lockId, CancellationTokenSource? cts)
        {
            Envelope = envelope;
            _cancellationToken = cts?.Token ?? CancellationToken.None;
            _cts = cts;
            LockId = lockId;
        }

        public OutboxEnvelope Envelope { get; }
        public Guid LockId { get; }
        public CancellationToken LockToken => _cancellationToken;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                NullableHelper.SetNull(ref _cts)?.Dispose();
            }
        }

        //public Task<bool> TryReleaseLockAsync(CancellationToken cancellationToken)
        //{
        //    CheckDisposed();

        //    return _releaseCallback.Invoke(_state, cancellationToken);
        //}

        //[MemberNotNull(nameof(_releaseCallback))]
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private void CheckDisposed()
        //{
        //    if (!_disposed)
        //    {
        //        //Debug.Assert(_releaseCallback != null);
        //        return;
        //    }
        //    Throw();
        //}

        //[DoesNotReturn]
        //[MethodImpl(MethodImplOptions.NoInlining)]
        //private void Throw()
        //{
        //    throw new ObjectDisposedException(GetType().Name);
        //}
    }
}
