using System;
using System.Linq;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.TransactionTests;

public class ImplementationReasoningTests : BaseTest
{
    [Test]
    public async Task Savepoint_Rollback_Causes_Silent_Reinsertion_Danger()
    {
        // Тест демонстрирует, что RollbackToSavepoint откатывает данные в БД,
        // но оставляет их в ChangeTracker, что ведет к тихой повторной вставке.

        // Включаем стратегию ретраев
        TestDbContext.IsRetryStrategy = true;

        var sp = InitServiceCollection().BuildServiceProvider();
        var context = sp.GetRequiredService<TestDbContext>();
        var strategy = context.Database.CreateExecutionStrategy();

        // Оборачиваем всё в ExecutionStrategy, так как при включенных ретраях EF запрещает ручной BeginTransactionAsync вне стратегии
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync();

            var entity1 = new TestUser {Id = Guid.NewGuid(), Name = "Initial User", Years = 25};
            context.Users.Add(entity1);
            await context.SaveChangesAsync();

            // 1. Создаем Savepoint
            await transaction.CreateSavepointAsync("AfterFirstEntity");

            var entity2Id = Guid.NewGuid();
            try
            {
                // 2. Добавляем данные, которые захотим откатить
                context.Users.Add(new TestUser {Id = entity2Id, Name = "Should Be Rolled Back", Years = 30});

                // 3. Имитируем ошибку
                throw new Exception("Simulated business error");
            }
            catch (Exception)
            {
                // 4. ОТКАТЫВАЕМ только до Savepoint в БД
                await transaction.RollbackToSavepointAsync("AfterFirstEntity");
            }

            // ПРОВЕРКА: В БД сущности НЕТ (БД откатилась успешно)
            var existsInDb = await context.Users.AsNoTracking().AnyAsync(u => u.Id == entity2Id);
            NUnit.Framework.Assert.That(existsInDb, Is.False, "В базе данных сущности нет после RollbackToSavepoint");

            // ПРОВЕРКА: В ChangeTracker сущность ЕСТЬ (EF про откат в БД не знает!)
            var isTracked = context.ChangeTracker.Entries<TestUser>().Any(e => e.Entity.Id == entity2Id);
            NUnit.Framework.Assert.That(isTracked, Is.True, "ОПАСНОСТЬ! В ChangeTracker сущность осталась в состоянии Added");

            // 5. ТИХАЯ ОШИБКА: Сохраняем что-то другое
            context.Users.Add(new TestUser {Id = Guid.NewGuid(), Name = "Third User", Years = 35});

            // Этот вызов заново вставит entity2, хотя мы думали, что откатили её!
            await context.SaveChangesAsync();

            // ДОКАЗАТЕЛЬСТВО: Сущность "воскресла" в БД
            var reinserted = await context.Users.AsNoTracking().AnyAsync(u => u.Id == entity2Id);
            NUnit.Framework.Assert.That(reinserted, Is.True, "КРИТИЧЕСКАЯ ОШИБКА: Сущность была повторно вставлена в БД из грязного трекера!");

            await transaction.CommitAsync();
        });
    }

    [Test]
    public Task Strategy_Retry_Fails_Due_To_Dirty_Tracker_With_Real_Strategy()
    {
        try
        {
            // Тест демонстрирует, что если стратегия ретрая не чистит трекер,
            // вторая попытка упадет сразу же при попытке добавить тот же объект.

            // Включаем стратегию ретраев
            TestDbContext.IsRetryStrategy = true;

            var sp = InitServiceCollection().BuildServiceProvider();
            var context = sp.GetRequiredService<TestDbContext>();
            var strategy = context.Database.CreateExecutionStrategy();

            var userId = Guid.NewGuid();
            var attempt = 0;

            // Ожидаем, что стратегия выбросит InvalidOperationException
            var ex = NUnit.Framework.Assert.ThrowsAsync<InvalidOperationException>((Func<Task>)(async () =>
            {
                await strategy.ExecuteAsync(async () =>
                {
                    attempt++;
                    await using var transaction = await context.Database.BeginTransactionAsync();

                    // На обеих попытках пробуем добавить пользователя с одним и тем же ID
                    context.Users.Add(new TestUser {Id = userId, Name = $"RetryUser_At_{attempt}", Years = 20});

                    if (attempt == 1)
                    {
                        // Имитируем РЕАЛЬНУЮ временную ошибку (Transient error).
                        // Только при такой ошибке ExecutionStrategy пойдет на вторую попытку.
                        throw new TimeoutException("Simulated database timeout (transient error)");
                    }

                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();
                });
            }));

            NUnit.Framework.Assert.That(attempt, Is.EqualTo(2), "Должно было быть сделано 2 попытки (1-я упала по таймауту, 2-я по трекеру)");
            NUnit.Framework.Assert.That(ex!.Message, Does.Contain("already being tracked"),
                "Вторая попытка упала не из-за БД, а из-за того, что трекер грязный с первой попытки");
            return Task.CompletedTask;
        }
        catch (Exception exception)
        {
            return Task.FromException(exception);
        }
    }
}