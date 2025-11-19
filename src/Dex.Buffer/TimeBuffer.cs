using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Dex.Buffer
{
    public class TimeBuffer<T> : IDisposable
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        private readonly TimeSpan _period;
        private readonly Action<T[]> _onFlush;
        private readonly Timer _timer;
        private bool _isDisposed;

        public TimeBuffer(TimeSpan period, Action<T[]> onFlush)
        {
            _onFlush = onFlush ?? throw new ArgumentNullException(nameof(onFlush));
            _period = period;
            _timer = new Timer(_ => Process(), null, Timeout.Infinite, Timeout.Infinite);
            _timer.Change((int)period.TotalMilliseconds, Timeout.Infinite);
            _isDisposed = false;
        }

        public void Enqueue(T obj)
        {
            _queue.Enqueue(obj);
        }

        public void Flush(int maxCount = 5000)
        {
            if (maxCount <= 0) throw new ArgumentOutOfRangeException(nameof(maxCount));

            if (_queue.IsEmpty)
            {
                return;
            }

            var ar = new List<T>();

            while (ar.Count < maxCount && _queue.TryDequeue(out var result))
            {
                ar.Add(result);
            }

            _onFlush(ar.ToArray()); // TODO а что случится если делегат упадет, похоже мы теряем логи..
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing) return;

            _timer.Dispose();

            _isDisposed = true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031")]
        private void Process()
        {
            try
            {
                Flush();
            }
            catch (Exception ex)
            {
                TraceString($"TimeBuffer error. Exception: {ex.Message}, Stack: {ex.StackTrace}");
            }
            finally
            {
                _timer.Change((int)_period.TotalMilliseconds, Timeout.Infinite);
            }
        }

        private static void TraceString(string str)
        {
            System.Diagnostics.Trace.TraceError(str);
        }
    }
}