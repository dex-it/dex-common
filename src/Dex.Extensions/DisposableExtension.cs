using System;
using System.Collections.Generic;
using System.Linq;

namespace Dex.Extensions
{
    public static class DisposableExtension
    {
        [Obsolete]
        public static void SafeDispose(this IEnumerable<IDisposable?> collection, Action<IDisposable, Exception>? exceptionHandler = null)
        {
            foreach (var disposable in collection.Where(disposable => disposable != null))
            {
                SafeDispose(disposable, exceptionHandler);
            }
        }

        [Obsolete]
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design","CA1031")]
        public static void SafeDispose(this IDisposable? disposable, Action<IDisposable, Exception>? exceptionHandler = null)
        {
            try
            {
                disposable?.Dispose();
            }
            catch (Exception ex)
            {
                if (exceptionHandler == null) return;

                try
                {
                    exceptionHandler(disposable ?? throw new ArgumentNullException(nameof(disposable)), ex);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}