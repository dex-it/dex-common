using System;
using System.Linq;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Ef.Tests.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.TransactionTests;

public class CqrsTransactionTests : BaseTest
{
    [Test]
    public async Task Cqrs_CrossContext_Isolation_And_NoDtc_Promotion_Test()
    {
        // This test verifies two key aspects of our explicit transaction implementation:
        // 1. Isolation: One DbContext instance (replica) does NOT see uncommitted changes from another 
        //    DbContext instance (master) even when running within the same logical block.
        // 2. No DTC Promotion: Working with multiple DbContext instances (and different connections) 
        //    does NOT trigger "Ambient transaction detected" errors or unwanted DTC escalation,
        //    because we use IDbContextTransaction instead of TransactionScope.
        // 3. Data Availability: The replica instance sees the master's data immediately after the master's commit.

        var sp = InitServiceCollection().BuildServiceProvider();
        var masterContext = sp.GetRequiredService<TestDbContext>();
        await using var replicaContext = new TestDbContext(DbName);

        var userId = Guid.NewGuid();
        var userName = "CQRS_User";

        // Используем явную транзакцию на мастере
        await masterContext.ExecuteInTransactionAsync(async ct =>
        {
            // Пишем в мастер
            masterContext.Users.Add(new TestUser { Id = userId, Name = userName, Years = 25 });
            await masterContext.SaveChangesAsync(ct);

            // Читаем из реплики ВНУТРИ транзакции мастера. 
            // Это "тот самый" момент, где TransactionScope мог упасть.
            var replicaUsersBeforeCommit = await replicaContext.Users.Where(x => x.Id == userId).ToArrayAsync(ct);

            // Проверка изоляции: реплика не должна видеть изменения до коммита
            NUnit.Framework.Assert.That(replicaUsersBeforeCommit, Is.Empty, "Replica must not see uncommitted master data.");

            return true;
        }, _ => Task.FromResult(true));

        // Проверка после коммита
        var replicaUsersAfterCommit = await replicaContext.Users.Where(x => x.Id == userId).ToArrayAsync();
        NUnit.Framework.Assert.That(replicaUsersAfterCommit, Is.Not.Empty, "Replica must see committed data.");
        NUnit.Framework.Assert.That(replicaUsersAfterCommit[0].Name, Is.EqualTo(userName));
    }

    [Test]
    public async Task Cqrs_MasterWrite_ReplicaRead_DirectQuery_IsSupported_Test()
    {
        // This test explicitly confirms that direct queries to a second DbContext (e.g. read replica) 
        // are perfectly fine within a Master's ExecuteInTransactionAsync block, 
        // as they don't trigger the multi-context protection logic.

        var sp = InitServiceCollection().BuildServiceProvider();
        var masterContext = sp.GetRequiredService<TestDbContext>();
        await using var replicaContext = new TestDbContext(DbName);

        await masterContext.ExecuteInTransactionAsync(async ct =>
        {
            masterContext.Users.Add(new TestUser { Name = "MasterEntry" });
            await masterContext.SaveChangesAsync(ct);

            // Direct query to replica is ALLOWED and works.
            var replicaData = await replicaContext.Users.AnyAsync(ct);
            NUnit.Framework.Assert.That(replicaData, Is.Not.Null);

            return true;
        }, _ => Task.FromResult(true));
    }

    [Test]
    public async Task Cqrs_MasterWrite_ReplicaRead_ExecuteInTransaction_On_Replica_IsForbidden_Test()
    {
        // This test proves that while direct reads from a replica are allowed (see Cqrs_CrossContext_Isolation_And_NoDtc_Promotion_Test),
        // wrapping the replica call in a nested ExecuteInTransactionAsync is FORBIDDEN to prevent silent atomicity loss.

        var sp = InitServiceCollection().BuildServiceProvider();
        var masterContext = sp.GetRequiredService<TestDbContext>();
        await using var replicaContext = new TestDbContext(DbName);

        await masterContext.ExecuteInTransactionAsync(async ct =>
        {
            masterContext.Users.Add(new TestUser { Name = "MasterUser" });
            await masterContext.SaveChangesAsync(ct);

            // Attempting to wrap replica work in ExecuteInTransactionAsync MUST throw.
            var ex = NUnit.Framework.Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await replicaContext.ExecuteInTransactionAsync(async ctReplica => { _ = await replicaContext.Users.AnyAsync(ctReplica); }, _ => Task.FromResult(true),
                    cancellationToken: ct);
            });

            NUnit.Framework.Assert.That(ex!.Message, Does.Contain("Detected a nested call to ExecuteInTransactionAsync using a different DbContext instance"));
            NUnit.Framework.Assert.That(ex.Message, Does.Contain("If you only need retry logic for the second context without a transaction"));

            return true;
        }, _ => Task.FromResult(true));
    }

    [Test]
    public async Task Transaction_Reentrancy_NestedCalls_JoinExistingTransaction_Test()
    {
        // Проверяем, что вложенные вызовы ExecuteInTransactionAsync работают в одной транзакции (реентрабельность).
        // Это важно, если один сервис вызывает другой, и оба хотят работать в транзакции.

        var sp = InitServiceCollection().BuildServiceProvider();
        var context = sp.GetRequiredService<TestDbContext>();

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await context.ExecuteInTransactionAsync(async ct =>
        {
            context.Users.Add(new TestUser { Id = id1, Name = "Outer", Years = 10 });
            await context.SaveChangesAsync(ct);

            // Вложенный вызов должен увидеть, что транзакция уже есть, и просто выполнить код в ней.
            await context.ExecuteInTransactionAsync(async ctInner =>
            {
                context.Users.Add(new TestUser { Id = id2, Name = "Inner", Years = 20 });
                await context.SaveChangesAsync(ctInner);
                return true;
            }, _ => Task.FromResult(true), cancellationToken: ct);

            // Проверяем, что внутри внешней транзакции видны оба изменения
            NUnit.Framework.Assert.That(await context.Users.CountAsync(x => x.Id == id1 || x.Id == id2, ct), Is.EqualTo(2));

            return true;
        }, _ => Task.FromResult(true));

        // Проверяем итоговое состояние в базе
        NUnit.Framework.Assert.That(await context.Users.CountAsync(x => x.Id == id1 || x.Id == id2), Is.EqualTo(2));
    }
}