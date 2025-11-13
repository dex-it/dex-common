using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;
using Microsoft.EntityFrameworkCore;

namespace Dex.Cap.Ef.Tests.OnceExecutorTests.Strategies;

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class Concrete3ExecutionStrategy : IOnceExecutionStrategy<Concrete3ExecutionStrategyRequest, IEfTransactionOptions, TestUser>
{
    private readonly TestDbContext _dbContext;

    public IEfTransactionOptions? Options { get; set; }

    public Concrete3ExecutionStrategy(TestDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsAlreadyExecutedAsync(Concrete3ExecutionStrategyRequest argument, CancellationToken cancellationToken)
    {
        var userDb = await _dbContext.Users.SingleOrDefaultAsync(x => x.Name == argument.Value, cancellationToken);
        return userDb != null;
    }

    public async Task ExecuteAsync(Concrete3ExecutionStrategyRequest argument, CancellationToken cancellationToken)
    {
        var user = new TestUser { Name = argument.Value, Years = 18 };
        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TestUser?> ReadAsync(Concrete3ExecutionStrategyRequest argument, CancellationToken cancellationToken)
    {
        var userDb = await _dbContext.Users.SingleOrDefaultAsync(x => x.Name == argument.Value, cancellationToken);
        return userDb;
    }
}