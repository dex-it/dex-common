using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;
using Dex.Cap.OnceExecutor.Ef;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.Ef.Tests.OnceExecutorTests.Strategies
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal sealed class Concrete2ExecutionStrategy : IOnceExecutionStrategy<Concrete2ExecutionStrategyRequest, IEfOptions, string>
    {
        private readonly TestDbContext _dbContext;

        public IEfOptions? Options { get; set; }

        public Concrete2ExecutionStrategy(TestDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> IsAlreadyExecutedAsync(Concrete2ExecutionStrategyRequest argument, CancellationToken cancellationToken)
        {
            var userDb = await _dbContext.Users.SingleOrDefaultAsync(x => x.Name == argument.Value, cancellationToken);
            return userDb != null;
        }

        public async Task ExecuteAsync(Concrete2ExecutionStrategyRequest argument, CancellationToken cancellationToken)
        {
            var user = new TestUser { Name = argument.Value, Years = 18 };
            await _dbContext.Users.AddAsync(user, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<string?> ReadAsync(Concrete2ExecutionStrategyRequest argument, CancellationToken cancellationToken)
        {
            var userDb = await _dbContext.Users.SingleOrDefaultAsync(x => x.Name == argument.Value, cancellationToken);
            return userDb?.Name;
        }
    }
}