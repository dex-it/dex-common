using Dex.Cap.Common.Ef.Interfaces;
using Dex.Cap.Outbox.Interfaces;
using MassTransit;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit.Extensions;

public static class ConsumeContextExtensions
{
    public static string GetIdempotentKey<TMessage>(this ConsumeContext<TMessage> context)
        where TMessage : class
    {
        if (context.MessageId == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Message is not IHaveIdempotenceKey k)
        {
            return context.MessageId.Value.ToString("N");
        }

        if (k is { IdempotentKey: null } && context.Message is IOutboxMessage o)
        {
            return o.MessageId.ToString("N");
        }

        return k.IdempotentKey ?? context.MessageId.Value.ToString("N");
    }
}