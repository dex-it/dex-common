using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.Ef.Tests.OutboxTests
{
    public class TestCreateUserCommandHandler : IOutboxMessageHandler<TestUserCreatorCommand>
    {
        private readonly DbContext _dbContext;

        public TestCreateUserCommandHandler(TestDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ProcessMessage(TestUserCreatorCommand message, CancellationToken cancellationToken)
        {
            _dbContext.Set<User>().Add(new User { Id = message.Id });
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public Task ProcessMessage(IOutboxMessage outbox, CancellationToken cancellationToken)
        {
            return ProcessMessage((TestUserCreatorCommand)outbox, cancellationToken);
        }
    }

    public class TestUserCreatorCommand : IOutboxMessage
    {
        public Guid Id { get; set; }
    }
}