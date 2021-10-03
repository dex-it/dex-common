using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Outbox.Interfaces;
using Dex.Cap.Outbox.Models;

namespace Dex.Cap.Outbox
{
    internal sealed class OutboxService : IOutboxService
    {
        private readonly IOutboxDataProvider _outboxDataProvider;
        private readonly IOutboxSerializer _serializer;

        public OutboxService(IOutboxDataProvider outboxDataProvider, IOutboxSerializer serializer)
        {
            _outboxDataProvider = outboxDataProvider ?? throw new ArgumentNullException(nameof(outboxDataProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async Task<Guid> ExecuteOperationAsync<TContext, TOutboxMessage>(Guid correlationId,
            Func<CancellationToken, Task<TContext>> usefulAction,
            Func<CancellationToken, TContext, Task<TOutboxMessage>> createOutboxData,
            CancellationToken cancellationToken = default)
            where TOutboxMessage : IOutboxMessage
        {
            if (usefulAction == null) throw new ArgumentNullException(nameof(usefulAction));
            if (createOutboxData == null) throw new ArgumentNullException(nameof(createOutboxData));

            await _outboxDataProvider.ExecuteUsefulAndSaveOutboxActionIntoTransaction(correlationId, usefulAction,
                    async (token, context) =>
                    {
                        var outboxData = await createOutboxData(token, context).ConfigureAwait(false);
                        await EnqueueAsync(correlationId, outboxData, token).ConfigureAwait(false);
                        return outboxData;
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            return correlationId;
        }

        public async Task<Guid> ExecuteOperationAsync<TContext, TOutboxMessage>(Guid correlationId,
            Func<CancellationToken, Task<TContext>> usefulAction,
            Func<TContext, TOutboxMessage> createOutboxData,
            CancellationToken cancellationToken = default)
            where TOutboxMessage : IOutboxMessage
        {
            if (usefulAction == null) throw new ArgumentNullException(nameof(usefulAction));
            if (createOutboxData == null) throw new ArgumentNullException(nameof(createOutboxData));

            await _outboxDataProvider.ExecuteUsefulAndSaveOutboxActionIntoTransaction(correlationId, usefulAction,
                    async (token, context) =>
                    {
                        var outboxData = createOutboxData(context);
                        await EnqueueAsync(correlationId, outboxData, token).ConfigureAwait(false);
                        return outboxData;
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            return correlationId;
        }

        public async Task<Guid> EnqueueAsync<T>(Guid correlationId, T message, CancellationToken cancellationToken) where T : IOutboxMessage
        {
            var assemblyQualifiedName = message.GetType().AssemblyQualifiedName;
            if (assemblyQualifiedName == null)
            {
                throw new InvalidOperationException("Can't resolve assemblyQualifiedName");
            }

            var outbox = new OutboxEnvelope(correlationId, assemblyQualifiedName, OutboxMessageStatus.New, _serializer.Serialize(message));
            await _outboxDataProvider.Add(outbox, cancellationToken).ConfigureAwait(false);
            return correlationId;
        }

        public async Task<Guid> EnqueueAsync<T>(T message, CancellationToken cancellationToken) where T : IOutboxMessage
        {
            return await EnqueueAsync(Guid.NewGuid(), message, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> IsOperationExistsAsync(Guid correlationId, CancellationToken cancellationToken)
        {
            return await _outboxDataProvider.IsExists(correlationId, cancellationToken).ConfigureAwait(false);
        }
    }
}