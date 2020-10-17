using System;
using System.Collections.Generic;
using System.Linq;

namespace Dex.Extensions
{
    public static class DisposableExtension
    {
        public static void SafeDispose(this IEnumerable<IDisposable> collection, Action<IDisposable, Exception> exceptionHandler = null)
        {
            if (collection == null) return;
            foreach (var disposable in collection.Where(disposable => disposable != null))
            {
                SafeDispose(disposable, exceptionHandler);
            }
        }

        public static void SafeDispose(this IDisposable disposable, Action<IDisposable, Exception> exceptionHandler = null)
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
                    exceptionHandler(disposable, ex);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}