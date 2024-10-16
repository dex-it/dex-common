using Dex.Cap.Common.Ef.Interfaces;
using Dex.Cap.Outbox.Interfaces;
using MassTransit;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit.Extensions;

public static class ConsumeContextExtensions
{
    public static string GetIdempotentKey<TMessage>(this ConsumeContext<TMessage> context)
        where TMessage : class
    {
        if (context.Message is not IHaveIdempotenceKey key)
        {
            return GetMessageIdValue(context.MessageId);
        }

        if (key is { IdempotentKey: null } && context.Message is IOutboxMessage outboxMessage)
        {
            return GetMessageIdValue(outboxMessage.MessageId);
        }

        return key.IdempotentKey ?? GetMessageIdValue(context.MessageId);

        string GetMessageIdValue(Guid? messageId)
        {
            if (messageId == null)
            {
                throw new ArgumentNullException(nameof(messageId));
            }

            return messageId.Value.ToString("N");
        }
    }
}