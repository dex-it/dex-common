using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox.Interfaces
{
    public interface IOutboxHandler
    {
        /// <exception cref="OperationCanceledException"/>
        Task ProcessAsync(CancellationToken cancellationToken = default);
    }
}