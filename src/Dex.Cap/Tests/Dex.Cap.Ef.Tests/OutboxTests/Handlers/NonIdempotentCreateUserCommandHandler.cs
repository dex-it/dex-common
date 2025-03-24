using System;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Outbox.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.Ef.Tests.OutboxTests.Handlers
{
    public class NonIdempotentCreateUserCommandHandler : IOutboxMessageHandler<TestUserCreatorCommand>
    {
        private readonly DbContext _dbContext;
        public static int CountDown { get; set; }

        public NonIdempotentCreateUserCommandHandler(TestDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Process(TestUserCreatorCommand message, CancellationToken cancellationToken)
        {
            _dbContext.Set<TestUser>().Add(new TestUser { Id = message.Id, Name = message.UserName });

            if (CountDown-- > 0)
                throw new InvalidOperationException("CountDown > 0");

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}