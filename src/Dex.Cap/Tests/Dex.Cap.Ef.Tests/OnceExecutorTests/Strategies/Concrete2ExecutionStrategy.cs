using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.Ef.Tests.Strategies
{
    public class Concrete2ExecutionStrategy : IConcrete2ExecutionStrategy
    {
        private readonly TestDbContext _dbContext;

        public Concrete2ExecutionStrategy(TestDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> CheckIdempotenceAsync(string argument, CancellationToken cancellationToken)
        {
            var userDb = await _dbContext.Users.SingleOrDefaultAsync(x => x.Name == argument, cancellationToken);
            return userDb != null;
        }

        public async Task ExecuteAsync(string argument, CancellationToken cancellationToken)
        {
            var user = new TestUser { Name = argument, Years = 18 };
            await _dbContext.Users.AddAsync(user, cancellationToken);
        }

        public async Task<string?> ReadAsync(string argument, CancellationToken cancellationToken)
        {
            var userDb = await _dbContext.Users.SingleOrDefaultAsync(x => x.Name == argument, cancellationToken);
            return userDb?.Name;
        }
    }
}