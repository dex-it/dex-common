using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Outbox.OnceExecutor.MassTransit;

namespace Dex.Cap.Ef.Tests.OutboxTests.Handlers;

public class TransactionalCreateUserCommandHandler(TestDbContext dbContext) : TransactionalOutboxHandler<TestUserCreatorCommand, TestDbContext>(dbContext)
{
    private readonly TestDbContext _dbContext = dbContext;

    protected override async Task ProcessInTransaction(TestUserCreatorCommand message,
        CancellationToken cancellationToken)
    {
        _dbContext.Set<TestUser>().Add(new TestUser { Id = message.Id, Name = message.UserName });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}