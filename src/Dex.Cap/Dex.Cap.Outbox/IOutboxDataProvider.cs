using System;
using System.Threading.Tasks;

namespace Dex.Cap.Outbox
{
    public interface IOutboxDataProvider
    {
        Task<Models.Outbox> Save(Models.Outbox outbox);
        Task<Models.Outbox[]> GetWaitingMessages();
        Task Fail(Models.Outbox outbox, string? errorMessage = null, Exception? exception = null);
        Task Succeed(Models.Outbox outbox);
    }
}