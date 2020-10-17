using System;
using System.Threading;

namespace Dex.Extensions
{
    public static class CancellationTokenExtension
    {
        public static CancellationTokenSource CreateLinkedSourceWithTimeout(this CancellationToken token, TimeSpan timeout)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout);
            return CancellationTokenSource.CreateLinkedTokenSource(token, cancellationTokenSource.Token);
        }

        public static CancellationTokenSource CreateLinkedSourceWithTimeout(this CancellationTokenSource tokenSource, TimeSpan timeout)
        {
            return CreateLinkedSourceWithTimeout(tokenSource.Token, timeout);
        }
    }
}