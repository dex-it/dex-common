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
    public async Task Cqrs_MasterWrite_ReplicaRead_Isolation_Test()
    {
        // Этот тест проверяет СРАЗУ ТРИ вещи:
        // 1. Изоляцию: Реплика не видит данные мастера до коммита.
        // 2. Отсутствие эскалации: Работа со вторым контекстом (репликой) внутри транзакции не вызывает ошибок DTC.
        // 3. Доступность: Реплика видит данные сразу после коммита мастера.

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