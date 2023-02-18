using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;
using Dex.Cap.Outbox.Interfaces;

namespace Dex.Cap.Ef.Tests.OutboxTests.Handlers
{
    public class IdempotentCreateUserCommandHandler : IOutboxMessageHandler<TestUserCreatorCommand>
    {
        private readonly IOnceExecutor<TestDbContext> _onceExecutor;
        public static int CountDown { get; set; }

        public bool IsTransactional => true;

        public IdempotentCreateUserCommandHandler(IOnceExecutor<TestDbContext> onceExecutor)
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

                    if (CountDown-- > 0)
                        throw new InvalidOperationException("CountDown > 0");

                    await context.SaveChangesAsync(token);
                },
                TransactionScopeOption.RequiresNew, // может быть любой уровень
                cancellationToken: cancellationToken);
        }

        public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
        {
            return ProcessMessage((TestUserCreatorCommand)outbox, cancellationToken);
        }
    }
}