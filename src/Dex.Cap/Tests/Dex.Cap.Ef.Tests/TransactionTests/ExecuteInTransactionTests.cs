using System;
using System.Linq;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Outbox.Interfaces;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.TransactionTests;

public class ExecuteInTransactionTests : BaseTest
{
    [Test]
    public async Task SimpleRunExecuteInTransactionTest()
    {
        var sp = InitServiceCollection()
            .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
            .BuildServiceProvider();

        var count = 0;
        EventHandler<TestOutboxCommand> onProcess = (_, _) => { count++; };
        TestCommandHandler.OnProcess += onProcess;
        try
        {
            var outboxService = sp.GetRequiredService<IOutboxService>();
            var dbContext = sp.GetRequiredService<TestDbContext>();

            // act
            var name = "mmx_" + Guid.NewGuid();
            await dbContext.ExecuteInTransactionAsync(
                (dbContext, outboxService),
                async (state, token) =>
                {
                    await state.dbContext.Users.AddAsync(new TestUser { Name = name }, token);
                    await state.outboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, cancellationToken: token);
                    await state.dbContext.SaveChangesAsync(token);
                },
                (_, _) => Task.FromResult(false));

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync();

            // check
            Assert.AreEqual(1, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
        }
        finally
        {
            TestCommandHandler.OnProcess -= onProcess;
        }
    }

    [Test]
    public async Task RetryExecuteInTransactionTest()
    {
        var sp = InitServiceCollection()
            .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
            .BuildServiceProvider();

        var count = 0;
        EventHandler<TestOutboxCommand> onProcess = (_, _) => { count++; };
        TestCommandHandler.OnProcess += onProcess;
        try
        {
            var outboxService = sp.GetRequiredService<IOutboxService>();
            var dbContext = sp.GetRequiredService<TestDbContext>();
            var failureCount = 2;

            // act
            var name = "mmx_" + Guid.NewGuid();
            await dbContext.ExecuteInTransactionAsync(
                (dbContext, outboxService),
                async (state, token) =>
                {
                    await state.dbContext.Users.AddAsync(new TestUser { Name = name }, token);

                    if (failureCount-- > 0)
                    {
                        TestContext.WriteLine("throw failure...");
                        throw new TimeoutException();
                    }

                    await state.outboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" },
                        cancellationToken: token);
                    await state.dbContext.SaveChangesAsync(token);
                }, (_, _) => Task.FromResult(false));

            var handler = sp.GetRequiredService<IOutboxHandler>();
            await handler.ProcessAsync();

            // check
            Assert.AreEqual(1, count);
            Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
        }
        finally
        {
            TestCommandHandler.OnProcess -= onProcess;
        }
    }

    [Test]
    public async Task NestedExecuteInTransactionTest()
    {
        var sp = InitServiceCollection()
            .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
            .BuildServiceProvider();

        var dbContext = sp.GetRequiredService<TestDbContext>();

        var name1 = "user1_" + Guid.NewGuid();
        var name2 = "user2_" + Guid.NewGuid();

        // act
        await dbContext.ExecuteInTransactionAsync(async token =>
        {
            await dbContext.Users.AddAsync(new TestUser { Name = name1 }, token);
            await dbContext.SaveChangesAsync(token);

            // Nested call
            await dbContext.ExecuteInTransactionAsync(async ct =>
            {
                await dbContext.Users.AddAsync(new TestUser { Name = name2 }, ct);
                await dbContext.SaveChangesAsync(ct);
            }, _ => Task.FromResult(false), cancellationToken: token);
        }, _ => Task.FromResult(false));

        // check
        Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name1));
        Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name2));
    }

    [Test]
    public async Task RetryStrategyExecuteInTransactionTest()
    {
        TestDbContext.IsRetryStrategy = true; // explicitly enable retry strategy (EnableRetryOnFailure)
        try
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

            var count = 0;
            EventHandler<TestOutboxCommand> onProcess = (_, _) => { count++; };
            TestCommandHandler.OnProcess += onProcess;
            try
            {
                var dbContext = sp.GetRequiredService<TestDbContext>();
                var outboxService = sp.GetRequiredService<IOutboxService>();

                var failureCount = 1;
                var name = "retry_strat_" + Guid.NewGuid();

                // act
                await dbContext.ExecuteInTransactionAsync(
                    async token =>
                    {
                        await dbContext.Users.AddAsync(new TestUser { Name = name }, token);

                        if (failureCount-- > 0)
                        {
                            TestContext.WriteLine("Simulating transient failure for RetryStrategy...");
                            // TimeoutException is usually treated as transient by EF strategies
                            throw new TimeoutException("Simulated transient error");
                        }

                        await outboxService.EnqueueAsync(new TestOutboxCommand { Args = "hello world" }, cancellationToken: token);
                        await dbContext.SaveChangesAsync(token);
                    },
                    _ => Task.FromResult(false));

                var handler = sp.GetRequiredService<IOutboxHandler>();
                await handler.ProcessAsync();

                // check
                Assert.AreEqual(1, count);
                Assert.IsTrue(await dbContext.Users.AnyAsync(x => x.Name == name));
            }
            finally
            {
                TestCommandHandler.OnProcess -= onProcess;
            }
        }
        finally
        {
            TestDbContext.IsRetryStrategy = false; // restore default
        }
    }

    [Test]
    public async Task ExecuteInTransactionAsync_Should_Throw_When_Nested_IsolationLevel_Is_Stricter()
    {
        var sp = InitServiceCollection().BuildServiceProvider();
        var dbContext = sp.GetRequiredService<TestDbContext>();

        // Start outer transaction with ReadCommitted
        await dbContext.ExecuteInTransactionAsync(ct =>
        {
            try
            {
                // Try to start inner transaction with Serializable (stricter than ReadCommitted)
                var options = new EfTransactionOptions
                {
                    IsolationLevel = System.Transactions.IsolationLevel.Serializable
                };

                var ex = NUnit.Framework.Assert.ThrowsAsync<InvalidOperationException>(async () =>
                {
                    await dbContext.ExecuteInTransactionAsync(
                        _ => Task.CompletedTask,
                        _ => Task.FromResult(true),
                        options,
                        ct);
                });

                NUnit.Framework.Assert.That(ex!.Message, Does.Contain("Can't participate in existing transaction with isolation level 'ReadCommitted'"));
                NUnit.Framework.Assert.That(ex.Message, Does.Contain("Requested level 'Serializable' is stricter"));
                return Task.CompletedTask;
            }
            catch (Exception exception)
            {
                return Task.FromException(exception);
            }
        }, _ => Task.FromResult(false), new EfTransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted });
    }

    [Test]
    public async Task ExecuteInTransactionAsync_Should_Rollback_To_Savepoint_On_Nested_Failure()
    {
        var sp = InitServiceCollection().BuildServiceProvider();
        var context = sp.GetRequiredService<TestDbContext>();

        var outerId = Guid.NewGuid();
        var innerId = Guid.NewGuid();

        await context.ExecuteInTransactionAsync(async ct =>
        {
            // Outer operation: add user
            context.Users.Add(new TestUser { Id = outerId, Name = "OuterUser", Years = 40 });
            await context.SaveChangesAsync(ct);

            // Nested operation that FAILS
            try
            {
                await context.ExecuteInTransactionAsync(async ctInner =>
                {
                    context.Users.Add(new TestUser { Id = innerId, Name = "InnerUser (SHOULD BE ROLLED BACK)", Years = 10 });
                    await context.SaveChangesAsync(ctInner);

                    throw new Exception("Nested failure!");
                }, _ => Task.FromResult(true), cancellationToken: ct);
            }
            catch (Exception ex) when (ex.Message == "Nested failure!")
            {
                // We caught the nested exception. 
                // Because of Savepoints, only 'innerId' user should be rolled back on the DB side.
                // IMPORTANT: EF Core doesn't automatically clear entities from ChangeTracker on Savepoint rollback.
                // But since we threw an exception, the next SaveChangesAsync in the outer block 
                // would fail if we didn't remove the failed entity from ChangeTracker.
                var entry = context.ChangeTracker.Entries<TestUser>().FirstOrDefault(x => x.Entity.Id == innerId);
                if (entry != null)
                {
                    entry.State = EntityState.Detached;
                }
            }

            // Verify: outer user is still present, inner is NOT.
            NUnit.Framework.Assert.That(await context.Users.AnyAsync(x => x.Id == outerId, ct), Is.True);
            NUnit.Framework.Assert.That(await context.Users.AnyAsync(x => x.Id == innerId, ct), Is.False);

            return true;
        }, _ => Task.FromResult(true));

        // Final check after commit
        NUnit.Framework.Assert.That(await context.Users.AnyAsync(x => x.Id == outerId), Is.True);
        NUnit.Framework.Assert.That(await context.Users.AnyAsync(x => x.Id == innerId), Is.False);
    }

    [Test]
    public async Task ExecuteInTransactionAsync_Should_Use_VerifySucceeded_To_Avoid_Duplicates_On_Retry()
    {
        var sp = InitServiceCollection().BuildServiceProvider();
        var context = sp.GetRequiredService<TestDbContext>();

        var userId = Guid.NewGuid();
        var attemptCount = 0;

        // Задача: проверить, что если verifySucceeded находит данные в БД (имитация "успешного коммита с потерей ACK"),
        // то стратегия НЕ делает повторную попытку.
        _ = await context.ExecuteInTransactionAsync(
            state: new { UserId = userId },
            operation: async (st, ct) =>
            {
                attemptCount++;

                context.Users.Add(new TestUser { Id = st.UserId, Name = "VerifyUser", Years = 25 });
                await context.SaveChangesAsync(ct);

                // Имитируем "Lost ACK": 
                // 1. Мы реально комитим транзакцию вручную (БД теперь знает о пользователе).
                await context.Database.CurrentTransaction!.CommitAsync(ct);

                // 2. Бросаем транзиентное исключение, как будто сеть упала СРАЗУ ПОСЛЕ коммита.
                throw new Npgsql.NpgsqlException("Fake network failure after commit",
                    new Npgsql.PostgresException("Error", "Severity", "Invariant", "57P01"));
#pragma warning disable CS0162 // Unreachable code detected
                return true;
#pragma warning restore CS0162
            },
            verifySucceeded: async (st, ct) =>
            {
                // Проверяем БД через новый Scope (честный запрос к базе).
                using var scope = sp.CreateScope();
                var checkContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                return await checkContext.Users.AnyAsync(x => x.Id == st.UserId, ct);
            }
        );

        NUnit.Framework.Assert.Multiple(() =>
        {
            // Операция должна была быть вызвана только ОДИН раз.
            NUnit.Framework.Assert.That(attemptCount, Is.EqualTo(1), "Operation should NOT be retried if verifySucceeded returned true");
        });

        // Финальная проверка: пользователь действительно в базе.
        var userExists = await context.Users.AnyAsync(x => x.Id == userId);
        NUnit.Framework.Assert.That(userExists, Is.True);
    }
}