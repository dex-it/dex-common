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
        // Этот тест проверяет два ключевых аспекта нашей реализации явных транзакций:
        // 1. Изоляция: Один инстанс DbContext (реплика) НЕ видит незафиксированные изменения из другого 
        //    инстанса DbContext (мастер), даже если они работают в рамках одного логического блока.
        // 2. Отсутствие эскалации DTC: Работа с несколькими инстансами DbContext (и разными соединениями) 
        //    НЕ вызывает ошибок "Ambient transaction detected" или нежелательной эскалации до DTC,
        //    так как мы используем IDbContextTransaction вместо TransactionScope.
        // 3. Доступность данных: Реплика видит данные мастера сразу после коммита мастера.

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
        }, _ => Task.FromResult(false));

        // Проверка после коммита
        var replicaUsersAfterCommit = await replicaContext.Users.Where(x => x.Id == userId).ToArrayAsync();
        NUnit.Framework.Assert.That(replicaUsersAfterCommit, Is.Not.Empty, "Replica must see committed data.");
        NUnit.Framework.Assert.That(replicaUsersAfterCommit[0].Name, Is.EqualTo(userName));
    }

    [Test]
    public async Task Cqrs_MasterWrite_ReplicaRead_DirectQuery_IsSupported_Test()
    {
        // Этот тест явно подтверждает, что прямые запросы ко второму DbContext (например, read-реплике) 
        // отлично работают внутри блока ExecuteInTransactionAsync мастера, 
        // так как они не триггерят логику защиты от многоконтекстных транзакций.

        var sp = InitServiceCollection().BuildServiceProvider();
        var masterContext = sp.GetRequiredService<TestDbContext>();
        await using var replicaContext = new TestDbContext(DbName);

        await masterContext.ExecuteInTransactionAsync(async ct =>
        {
            masterContext.Users.Add(new TestUser { Name = "MasterEntry" });
            await masterContext.SaveChangesAsync(ct);

            // Прямой запрос к реплике РАЗРЕШЕН и работает.
            var replicaData = await replicaContext.Users.AnyAsync(ct);
            NUnit.Framework.Assert.That(replicaData, Is.Not.Null);

            return true;
        }, _ => Task.FromResult(false));
    }

    [Test]
    public async Task Cqrs_MasterWrite_ReplicaRead_ExecuteInTransaction_On_Replica_IsForbidden_Test()
    {
        // Этот тест доказывает, что в то время как прямые чтения из реплики разрешены (см. тест выше),
        // обертывание работы с репликой во вложенный ExecuteInTransactionAsync ЗАПРЕЩЕНО 
        // для предотвращения скрытой потери атомарности.

        var sp = InitServiceCollection().BuildServiceProvider();
        var masterContext = sp.GetRequiredService<TestDbContext>();
        await using var replicaContext = new TestDbContext(DbName);

        await masterContext.ExecuteInTransactionAsync(async ct =>
        {
            masterContext.Users.Add(new TestUser { Name = "MasterUser" });
            await masterContext.SaveChangesAsync(ct);

            // Попытка обернуть работу с репликой в ExecuteInTransactionAsync ДОЛЖНА выбросить исключение.
            var ex = NUnit.Framework.Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await replicaContext.ExecuteInTransactionAsync(async ctReplica => { _ = await replicaContext.Users.AnyAsync(ctReplica); }, _ => Task.FromResult(false),
                    cancellationToken: ct);
            });

            NUnit.Framework.Assert.That(ex!.Message, Does.Contain("Detected a nested call to ExecuteInTransactionAsync using a different DbContext instance"));
            NUnit.Framework.Assert.That(ex.Message, Does.Contain("If you only need retry logic for the second context without a transaction"));

            return true;
        }, _ => Task.FromResult(false));
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
            }, _ => Task.FromResult(false), cancellationToken: ct);

            // Проверяем, что внутри внешней транзакции видны оба изменения
            NUnit.Framework.Assert.That(await context.Users.CountAsync(x => x.Id == id1 || x.Id == id2, ct), Is.EqualTo(2));

            return true;
        }, _ => Task.FromResult(false));

        // Проверяем итоговое состояние в базе
        NUnit.Framework.Assert.That(await context.Users.CountAsync(x => x.Id == id1 || x.Id == id2), Is.EqualTo(2));
    }
}