using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;
using Dex.Cap.Outbox.OnceExecutor.MassTransit;

namespace Dex.Cap.Ef.Tests.OutboxTests.Handlers;

public class IdempotentCreateUserCommandHandler(
    IOnceExecutor<IEfTransactionOptions, TestDbContext> onceExecutor,
    TestDbContext dbContext)
    : IdempotentOutboxHandler<TestUserCreatorCommand, TestDbContext>(onceExecutor)
{
    public static int CountDown { get; set; }

    protected override async Task IdempotentProcess(TestUserCreatorCommand message,
        CancellationToken cancellationToken)
    {
        dbContext.Set<TestUser>().Add(new TestUser { Id = message.Id, Name = message.UserName });

        await dbContext.SaveChangesAsync(cancellationToken);

        if (CountDown-- > 0)
            throw new InvalidOperationException("CountDown > 0");
    }
}