using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Dex.Extensions
{
    public static class CancellationTokenExtension
    {
        [SuppressMessage("Microsoft.Reliability", "CA2000")]
        public static CancellationTokenSource CreateLinkedSourceWithTimeout(this CancellationToken token, TimeSpan timeout)
        {
            var cancellationTokenSource = new CancellationTokenSource(timeout);
            cancellationTokenSource.Token.Register(() => cancellationTokenSource.Dispose());
            return CancellationTokenSource.CreateLinkedTokenSource(token, cancellationTokenSource.Token);
        }

        public static CancellationTokenSource CreateLinkedSourceWithTimeout(this CancellationTokenSource tokenSource, TimeSpan timeout)
        {
            if (tokenSource == null) throw new ArgumentNullException(nameof(tokenSource));
            return CreateLinkedSourceWithTimeout(tokenSource.Token, timeout);
        }
    }
}