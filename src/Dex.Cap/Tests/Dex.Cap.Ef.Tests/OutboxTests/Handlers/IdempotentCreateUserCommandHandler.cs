using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;
using Dex.Cap.OnceExecutor.Ef;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Ef.Tests.OutboxTests.Handlers
{
    public class IdempotentCreateUserCommandHandler : IOutboxMessageHandler<TestUserCreatorCommand>
    {
        private readonly IOnceExecutor<IEfOptions, TestDbContext> _onceExecutor;
        public static int CountDown { get; set; }

        public bool IsTransactional => true;

        public IdempotentCreateUserCommandHandler(IOnceExecutor<IEfOptions, TestDbContext> onceExecutor)
        {
            _onceExecutor = onceExecutor;
        }

        public async Task ProcessMessage(TestUserCreatorCommand message, CancellationToken cancellationToken)
        {
            await _onceExecutor.ExecuteAsync(
                message.MessageId.ToString("N"),
                async (context, token) =>
                {
                    context.Set<TestUser>().Add(new TestUser { Id = message.Id, Name = message.UserName });

                    await context.SaveChangesAsync(token);

                    if (CountDown-- > 0)
                        throw new InvalidOperationException("CountDown > 0");
                },
                cancellationToken: cancellationToken);
        }

        public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
        {
            return ProcessMessage((TestUserCreatorCommand)outbox, cancellationToken);
        }
    }
}