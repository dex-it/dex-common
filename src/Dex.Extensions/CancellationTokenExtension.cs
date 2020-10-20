using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Dex.Extensions
{
    public static class CancellationTokenExtension
    {
        public static CancellationTokenSource CreateLinkedSourceWithTimeout(this CancellationToken cancellationToken, TimeSpan timeout)
        {
            var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked.CancelAfter(timeout);
            return linked;
        }

        public static CancellationTokenSource CreateLinkedSourceWithTimeout(this CancellationTokenSource tokenSource, TimeSpan timeout)
        {
            if (tokenSource == null) 
                throw new ArgumentNullException(nameof(tokenSource));

            return CreateLinkedSourceWithTimeout(tokenSource.Token, timeout);
        }
    }
}