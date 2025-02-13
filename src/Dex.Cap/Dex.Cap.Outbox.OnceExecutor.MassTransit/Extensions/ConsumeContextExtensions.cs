using Dex.Cap.Common.Interfaces;
using MassTransit;

namespace Dex.Cap.Outbox.OnceExecutor.MassTransit.Extensions;

public static class ConsumeContextExtensions
{
    public static string GetIdempotentKey<TMessage>(this ConsumeContext<TMessage> context)
        where TMessage : class
    {
        if (context.Message is not IIdempotentKey key)
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