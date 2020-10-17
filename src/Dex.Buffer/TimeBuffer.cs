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

        public TimeBuffer(TimeSpan period, Action<T[]> onFlush)
        {
            _onFlush = onFlush ?? throw new ArgumentNullException(nameof(onFlush));
            _period = period;
            _timer = new Timer(state => Process(), null, Timeout.Infinite, Timeout.Infinite);
            _timer.Change((int) period.TotalMilliseconds, Timeout.Infinite);
        }

        public void Enqueue(T obj)
        {
            _queue.Enqueue(obj);
        }

        public void Flush(int maxCount = 5000)
        {
            if (maxCount <= 0) throw new ArgumentOutOfRangeException(nameof(maxCount));

            if (_queue.Count == 0)
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
            _timer.Dispose();
        }

        //

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
                _timer.Change((int) _period.TotalMilliseconds, Timeout.Infinite);
            }
        }

        private static void TraceString(string str)
        {
            System.Diagnostics.Trace.TraceError(str);
        }
    }
}