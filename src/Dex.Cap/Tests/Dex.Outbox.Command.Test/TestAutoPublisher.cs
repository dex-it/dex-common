using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Interfaces;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Outbox.Command.Test;

public class TestAutoPublisher<TMessage> : IOutboxMessageHandler<TMessage> where TMessage : class, IOutboxMessage
{
    public static bool IsAutoPublisher => true;

    public Task Process(TMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
}