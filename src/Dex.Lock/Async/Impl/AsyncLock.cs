using Dex.Lock.RwLock;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Lock.Async.Impl
{
    [DebuggerTypeProxy(typeof(DebugView))]
    [DebuggerDisplay("Taken = {_taken == 1 ? true : false}")]
    public sealed class AsyncLock : IAsyncLock
    {
        // Для добавления потока в очередь и удаления из очереди.
        private readonly object _syncObj = new object();

        /// <summary>
        /// Очередь пользовательских тасков, которые хотят получить блокировку.
        /// </summary>
        /// <remarks>Доступ через блокировку <see cref="_syncObj"/></remarks>
        private readonly WaitQueue _queue;

        /// <summary>
        /// Идентификатор потока у которого есть право освободить блокировку.
        /// Может только увеличиваться.
        /// </summary>
        /// <remarks>Превентивная защита от освобождения блокировки чужим потоком.</remarks>
        internal short _releaseTaskToken;

        /// <summary>
        /// Когда блокировка захвачена таском.
        /// </summary>
        /// <remarks>Модификация через блокировку <see cref="_syncObj"/> или атомарно.</remarks>
        internal int _taken;

        public AsyncLock()
        {
            _queue = new WaitQueue(this);
        }

        /// <summary>
        /// Выполняет блокировку задачи (Task), все задачи запущенные через LockAsync будут выполнятся последовательно
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public Task LockAsync(Func<Task> asyncAction)
        {
            if (asyncAction != null)
            {
                ValueTask<LockReleaser> lockTask = LockAsync();

                if (lockTask.IsCompletedSuccessfully)
                // Блокировка захвачена синхронно.
                {
                    IDisposable? releaser = lockTask.Result;
                    try
                    {
                        Task userTask = asyncAction();

                        if (userTask.IsCompletedSuccessfully())
                        // Пользовательский метод выполнился синхронно.
                        {
                            return Task.CompletedTask; // Освободит блокировку.
                        }
                        else
                        // Будем ждать пользовательский метод.
                        {
                            IDisposable releaserCopy = releaser;

                            // Предотвратить преждевременный Dispose.
                            releaser = null;
                            
                            return WaitUserActionAndRelease(userTask, releaserCopy);

                            static async Task WaitUserActionAndRelease(Task userTask, IDisposable releaser)
                            {
                                try
                                {
                                    await userTask.ConfigureAwait(false);
                                }
                                finally
                                {
                                    releaser.Dispose();
                                }
                            }
                        }
                    }
                    finally
                    {
                        releaser?.Dispose();
                    }
                }
                else
                // Блокировка занята другим потоком.
                {
                    return WaitLockAsync(lockTask.AsTask(), asyncAction);

                    static async Task WaitLockAsync(Task<LockReleaser> task, Func<Task> asyncAction)
                    {
                        IDisposable releaser = await task.ConfigureAwait(false);
                        try
                        {
                            await asyncAction().ConfigureAwait(false);
                        }
                        finally
                        {
                            releaser.Dispose();
                        }
                    }
                }
            }
            else
                throw new ArgumentNullException(nameof(asyncAction));
        }

        /// <summary>
        /// Action будет разделять блоировку совместно с задачами (Task) запущеными через LockAsync и будет выполнятся последовательно 
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public Task LockAsync(Action action)
        {
            if (action != null)
            {
                var task = LockAsync();
                if (task.IsCompletedSuccessfully)
                {
                    try
                    {
                        action();
                    }
                    finally
                    {
                        task.Result.Dispose();
                    }
                    return Task.CompletedTask;
                }
                else
                {
                    return WaitAsync(task.AsTask(), action);

                    static async Task WaitAsync(Task<LockReleaser> task, Action action)
                    {
                        IDisposable releaser = await task.ConfigureAwait(false);
                        try
                        {
                            action();
                        }
                        finally
                        {
                            releaser.Dispose();
                        }
                    }
                }
            }
            else
                throw new ArgumentNullException(nameof(action));
        }

        public ValueTask<LockReleaser> LockAsync()
        {
            // Попытка захватить блокировку атомарно.
            bool taken = Interlocked.CompareExchange(ref _taken, 1, 0) == 0;

            if (taken)
            // Захватили блокировку.
            {
                LockReleaser releaser = CreateNextReleaser();

                return new ValueTask<LockReleaser>(result: releaser);
            }
            else
            // Блокировка занята другим потоком.
            {
                lock (_syncObj)
                {
                    if (_taken == 1)
                    // Блокировка занята другим потоком -> становимся в очередь.
                    {
                        return new ValueTask<LockReleaser>(task: _queue.Enqueue());
                    }
                    else
                    // Блокировка уже освободилась -> захватили блокировку.
                    {
                        _taken = 1;

                        LockReleaser releaser = CreateNextReleaser();

                        return new ValueTask<LockReleaser>(result: releaser);
                    }
                }
            }
        }

        /// <summary>
        /// Освобождает блокировку по запросу пользователя.
        /// </summary>
        internal void ReleaseLock(LockReleaser releaser)
        {
            Debug.Assert(_taken == 1, "Нарушение порядка захвата блокировки");
            Debug.Assert(releaser.ReleaseToken == _releaseTaskToken, "Освобождения блокировки чужим потоком");

            lock (_syncObj)
            {
                if (releaser.ReleaseToken == _releaseTaskToken)
                // У текущего потока есть право освободить блокировку.
                {
                    // Запретить освобождать блокировку всем потокам.
                    InvalidateAllReleasers();

                    if (_queue.Count == 0)
                    // Больше потоков нет -> освободить блокировку.
                    {
                        Debug.Assert(_taken == 1);
                        _taken = 0;
                    }
                    else
                    // На блокировку претендуют другие потоки.
                    {
                        // Даём следующему потоку в очереди право на освобождение блокировки.
                        var nextReleaser = new LockReleaser(this, _releaseTaskToken);

                        // Передать владение блокировкой следующему потоку (разрешить войти в критическую секцию).
                        _queue.DequeueAndEnter(nextReleaser);
                    }
                }
            }
        }

        /// <summary>
        /// Увеличивает идентификатор что-бы инвалидировать все ранее созданные <see cref="LockReleaser"/>.
        /// </summary>
        /// <returns><see cref="LockReleaser"/> у которого есть эксклюзивное право освободить текущую блокировку.</returns>
        private LockReleaser CreateNextReleaser()
        {
            Debug.Assert(_taken == 1, "Блокировка должна быть захвачена");

            ++_releaseTaskToken;

            return new LockReleaser(this, _releaseTaskToken);
        }

        /// <summary>
        /// Предотвращает освобождение блокировки чужим потоком.
        /// </summary>
        /// <remarks>Увеличивает <see cref="_releaseTaskToken"/>.</remarks>
        private void InvalidateAllReleasers()
        {
            Debug.Assert(Monitor.IsEntered(_syncObj));
            Debug.Assert(_taken == 1);

            ++_releaseTaskToken;
        }

        internal sealed class WaitQueue
        {
            private readonly AsyncLock _context;
            /// <summary>
            /// Очередь ожидающий потоков (тасков) претендующих на захват блокировки.
            /// </summary>
            /// <remarks>Доступ только через блокировку <see cref="_context._syncObj"/>.</remarks>
            private readonly Queue<TaskCompletionSource<LockReleaser>> _queue = new Queue<TaskCompletionSource<LockReleaser>>();
            public int Count => _queue.Count;

            public WaitQueue(AsyncLock context)
            {
                _context = context;
            }

            internal Task<LockReleaser> Enqueue()
            {
                Debug.Assert(Monitor.IsEntered(_context._syncObj), "Выполнять можно только в блокировке");

                var tcs = new TaskCompletionSource<LockReleaser>(TaskCreationOptions.RunContinuationsAsynchronously);
                _queue.Enqueue(tcs); // Добавить в конец.
                return tcs.Task;
            }

            internal void DequeueAndEnter(LockReleaser releaser)
            {
                Debug.Assert(Monitor.IsEntered(_context._syncObj), "Выполнять можно только в блокировке");
                Debug.Assert(_queue.Count > 0);

                // Взять первый поток в очереди.
                var tcs = _queue.Dequeue();

                bool success = tcs.TrySetResult(releaser);
                Debug.Assert(success);
            }
        }

        /// <summary>A debugger view of the <see cref="AsyncLock"/></summary>
        [DebuggerNonUserCode]
        private sealed class DebugView
        {
            private readonly AsyncLock _self;

            public DebugView(AsyncLock self)
            {
                _self = self;
            }

            /// <summary>
            /// Сколько потоков ожидают блокировку.
            /// </summary>
            public int PendingTasks => _self._queue.Count;
        }
    }
}