using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;
using Dex.Cap.Outbox.OnceExecutor.MassTransit;

namespace Dex.Cap.Ef.Tests.OutboxTests.Handlers;

public class IdempotentCreateUserCommandHandler : IdempotentOutboxHandler<TestUserCreatorCommand, TestDbContext>
{
    private readonly TestDbContext _dbContext;
    public static int CountDown { get; set; }

    public IdempotentCreateUserCommandHandler(IOnceExecutor<IEfTransactionOptions, TestDbContext> onceExecutor,
        TestDbContext dbContext) : base(onceExecutor)
    {
        _dbContext = dbContext;
    }

    protected override async Task IdempotentProcess(TestUserCreatorCommand message,
        CancellationToken cancellationToken)
    {
        _dbContext.Set<TestUser>().Add(new TestUser { Id = message.Id, Name = message.UserName });

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (CountDown-- > 0)
            throw new InvalidOperationException("CountDown > 0");
    }
}