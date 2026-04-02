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
    public async Task Cqrs_TransactionOnMaster_ReadFromReplica_Success()
    {
        var sp = InitServiceCollection().BuildServiceProvider();
        var masterContext = sp.GetRequiredService<TestDbContext>();
        await using var replicaContext = new TestDbContext(DbName);

        var userId = Guid.NewGuid();
        var userName = "CQRS_Test_User";

        await masterContext.ExecuteInTransactionAsync(
            async ct =>
            {
                masterContext.Users.Add(new TestUser { Id = userId, Name = userName, Years = 25 });
                await masterContext.SaveChangesAsync(ct);

                var replicaUsers = await replicaContext.Users.Where(x => x.Id == userId).ToArrayAsync(ct);
                NUnit.Framework.Assert.That(replicaUsers.Length, Is.EqualTo(0));

                return true;
            },
            _ => Task.FromResult(false));

        var finalReplicaUsers = await replicaContext.Users.Where(x => x.Id == userId).ToArrayAsync();
        NUnit.Framework.Assert.That(finalReplicaUsers.Length, Is.EqualTo(1));
        NUnit.Framework.Assert.That(finalReplicaUsers[0].Name, Is.EqualTo(userName));
    }

    [Test]
    public async Task Cqrs_TransactionOnMaster_ReadFromReplica_NoEscalation_Success()
    {
        var sp = InitServiceCollection().BuildServiceProvider();
        var masterContext = sp.GetRequiredService<TestDbContext>();
        await using var replicaContext = new TestDbContext(DbName);

        var userId = Guid.NewGuid();

        await masterContext.ExecuteInTransactionAsync(
            async ct =>
            {
                masterContext.Users.Add(new TestUser { Id = userId, Name = "NoEscalation", Years = 30 });
                await masterContext.SaveChangesAsync(ct);

                _ = await replicaContext.Users.Take(1).ToArrayAsync(ct);
                return true;
            },
            _ => Task.FromResult(false));

        NUnit.Framework.Assert.Pass();
    }

    [Test]
    public async Task MultipleDbContexts_SharedTransaction_ViaUseTransaction_Success()
    {
        var sp = InitServiceCollection().BuildServiceProvider();
        var masterContext = sp.GetRequiredService<TestDbContext>();
        await using var secondContext = new TestDbContext(DbName);

        var userId = Guid.NewGuid();

        await masterContext.ExecuteInTransactionAsync(
            async ct =>
            {
                masterContext.Users.Add(new TestUser { Id = userId, Name = "Explicit", Years = 18 });
                await masterContext.SaveChangesAsync(ct);

                var anyInReplica = await secondContext.Users.AnyAsync(x => x.Id == userId, ct);
                NUnit.Framework.Assert.That(anyInReplica, Is.False);

                return true;
            },
            _ => Task.FromResult(false));

        var result = await masterContext.Users.AnyAsync(x => x.Id == userId);
        NUnit.Framework.Assert.That(result, Is.True);
    }
}