using Dex.Lock.Async.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Dex.Lock.Async
{
    /// <summary>
    /// Держит блокировку <see cref="AsyncLock"/> до вызова Dispose.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    [DebuggerTypeProxy(typeof(DebugView))]
    [DebuggerDisplay("Disposed = {_context._releaseTaskToken != ReleaseToken}")]
    [SuppressMessage("Performance", "CA1815:Переопределите операторы Equals и равенства для типов значений", Justification = "<Ожидание>")]
    public readonly struct LockReleaser : IDisposable
    {
        private readonly AsyncLock _context;
        /// <summary>
        /// Токен который изначально имел право выполнить освобождение блокировки.
        /// </summary>
        /// <remarks>Сверяется с <see cref="AsyncLock._releaseTaskToken"/> в момент Dispose 
        /// для проверки права освобождения блокировки (предотвращение повторного Dispose).</remarks>
        internal readonly short ReleaseToken;

        internal LockReleaser(AsyncLock parent, short token)
        {
            _context = parent;
            ReleaseToken = token;
        }

        /// <summary>
        /// Освобождает захваченную блокировку.
        /// </summary>
        /// <remarks>Потокобезопасно.</remarks>
        [DebuggerStepThrough]
        public void Dispose() => _context.ReleaseLock(this);

        [DebuggerNonUserCode]
        private sealed class DebugView
        {
            private readonly LockReleaser _self;

            public DebugView(LockReleaser self)
            {
                _self = self;
            }

            public AsyncLock Locker => _self._context;
            public bool Disposed => _self._context._releaseTaskToken != _self.ReleaseToken;
        }
    }
}
