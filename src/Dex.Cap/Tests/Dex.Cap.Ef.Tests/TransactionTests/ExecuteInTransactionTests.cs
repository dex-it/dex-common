using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Outbox.Interfaces;
using Dex.Outbox.Command.Test;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.TransactionTests;

public class ExecuteInTransactionTests : BaseTest
{
    [TearDown]
    public void ResetInterceptor()
    {
        TestDbContext.Interceptor = null;
    }

    /// <summary>
    /// Коммит прошёл, подтверждение потерялось: verifySucceeded находит работу сделанной, операция не
    /// переигрывается, и вызывающий обязан получить значение, которое операция посчитала.
    /// </summary>
    /// <remarks>
    /// Соседний тест на verifySucceeded бросает исключение ИЗ САМОЙ операции, поэтому значения там не
    /// возникает вовсе и возвращать нечего. Здесь операция отрабатывает целиком, а падает уже фиксация,
    /// то есть воспроизводится реальный путь: значение посчитано, но результат коммита неизвестен.
    /// </remarks>
    [Test]
    public async Task ExecuteInTransactionAsync_WhenAckIsLostAfterCommit_ReturnsWhatTheOperationProduced()
    {
        TestDbContext.IsRetryStrategy = true;
        TestDbContext.Interceptor = new AckLossAfterCommitInterceptor();
        try
        {
            var sp = InitServiceCollection().BuildServiceProvider();
            var context = sp.GetRequiredService<TestDbContext>();

            var userId = Guid.NewGuid();
            var attempts = 0;

            var produced = await context.ExecuteInTransactionAsync(
                state: new { UserId = userId },
                operation: async (st, ct) =>
                {
                    attempts++;
                    context.Users.Add(new TestUser { Id = st.UserId, Name = "AckLossUser", Years = 30 });
                    await context.SaveChangesAsync(ct);

                    return $"produced-on-attempt-{attempts}";
                },
                verifySucceeded: async (st, ct) =>
                {
                    using var scope = sp.CreateScope();
                    var checkContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                    return await checkContext.Users.AnyAsync(x => x.Id == st.UserId, ct);
                });

            Assert.AreEqual(1, attempts, "verifySucceeded reported the work as done, so the operation must not be retried");

            // Главное: без публикации результата в состояние здесь возвращался бы null, то есть вызывающий
            // читал бы «операция ничего не вернула» про работу, которая в БД зафиксирована.
            Assert.AreEqual("produced-on-attempt-1", produced,
                "the caller must get the value the operation produced, not default(TResult)");

            Assert.IsTrue(await context.Users.AnyAsync(x => x.Id == userId), "the row must really be committed");
        }
        finally
        {
            TestDbContext.IsRetryStrategy = false;
        }
    }

    /// <summary>
    /// Роняет первую фиксацию транзиентной ошибкой ПОСЛЕ того, как она успешно прошла.
    /// </summary>
    private sealed class AckLossAfterCommitInterceptor : DbTransactionInterceptor
    {
        private int _fired;

        public override Task TransactionCommittedAsync(
            DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Exchange(ref _fired, 1) == 0)
            {
                throw new NpgsqlException(
                    "Fake connection loss right after a successful commit",
                    new PostgresException("Error", "Severity", "Invariant", "57P01"));
            }

            return Task.CompletedTask;
        }
    }

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
        var count = 0;
        EventHandler<TestOutboxCommand> onProcess = (_, _) => { count++; };

        TestDbContext.IsRetryStrategy = true;
        TestCommandHandler.OnProcess += onProcess;
        try
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

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
            TestDbContext.IsRetryStrategy = false;
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
        var count = 0;
        EventHandler<TestOutboxCommand> onProcess = (_, _) => { count++; };

        TestDbContext.IsRetryStrategy = true; // explicitly enable retry strategy (EnableRetryOnFailure)
        TestCommandHandler.OnProcess += onProcess;
        try
        {
            var sp = InitServiceCollection()
                .AddScoped<IOutboxMessageHandler<TestOutboxCommand>, TestCommandHandler>()
                .BuildServiceProvider();

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
            TestDbContext.IsRetryStrategy = false; // restore default
        }
    }

    [Test]
    public async Task ExecuteInTransactionAsync_Should_Throw_When_Nested_IsolationLevel_Is_Stricter()
    {
        var sp = InitServiceCollection().BuildServiceProvider();
        var dbContext = sp.GetRequiredService<TestDbContext>();

        // Запускаем внешнюю транзакцию с ReadCommitted
        await dbContext.ExecuteInTransactionAsync(ct =>
        {
            try
            {
                // Пытаемся запустить внутреннюю транзакцию с Serializable (строже, чем ReadCommitted)
                var options = new EfTransactionOptions
                {
                    IsolationLevel = System.Transactions.IsolationLevel.Serializable
                };

                var ex = NUnit.Framework.Assert.ThrowsAsync<InvalidOperationException>((Func<Task>)(async () =>
                {
                    await dbContext.ExecuteInTransactionAsync(
                        _ => Task.CompletedTask,
                        _ => Task.FromResult(false),
                        options,
                        ct);
                }));

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
    public async Task ExecuteInTransactionAsync_Should_Use_VerifySucceeded_To_Avoid_Duplicates_On_Retry()
    {
        TestDbContext.IsRetryStrategy = true;
        try
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
                    throw new NpgsqlException("Fake network failure after commit",
                        new PostgresException("Error", "Severity", "Invariant", "57P01"));
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

            NUnit.Framework.Assert.Multiple((Action)(() =>
            {
                // Операция должна была быть вызвана только ОДИН раз.
                NUnit.Framework.Assert.That(attemptCount, Is.EqualTo(1), "Operation should NOT be retried if verifySucceeded returned true");
            }));

            // Финальная проверка: пользователь действительно в базе.
            var userExists = await context.Users.AnyAsync(x => x.Id == userId);
            NUnit.Framework.Assert.That(userExists, Is.True);
        }
        finally
        {
            TestDbContext.IsRetryStrategy = false;
        }
    }

    [Test]
    public async Task Nested_TransientRetry_NoIndependentRetries()
    {
        // Проверяем, что вложенный ExecuteInTransactionAsync НЕ создаёт собственный ExecutionStrategy,
        // а позволяет ошибке всплыть до корневой стратегии.

        TestDbContext.IsRetryStrategy = true;
        try
        {
            var sp = InitServiceCollection().BuildServiceProvider();
            var context = sp.GetRequiredService<TestDbContext>();

            var outerName = "outer_" + Guid.NewGuid();
            var innerName = "inner_" + Guid.NewGuid();
            var outerAttemptCount = 0;
            var innerAttemptCount = 0;

            // Act
            await context.ExecuteInTransactionAsync(async ct =>
            {
                outerAttemptCount++;

                // Outer: вставляем пользователя
                context.Users.Add(new TestUser { Name = outerName, Years = 30 });
                await context.SaveChangesAsync(ct);

                // Nested: вставляем другого пользователя, первая попытка — transient failure
                await context.ExecuteInTransactionAsync(async ctInner =>
                {
                    innerAttemptCount++;
                    TestContext.WriteLine($"Inner attempt #{innerAttemptCount}");

                    context.Users.Add(new TestUser { Name = innerName, Years = 20 });
                    await context.SaveChangesAsync(ctInner);

                    if (innerAttemptCount == 1)
                    {
                        // Симулируем transient ошибку.
                        // Если баг исправлен, эта ошибка ВЫЙДЕТ из вложенного метода
                        // и заставит ПЕРЕЗАПУСТИТЬ весь внешний блок.
                        throw new TimeoutException("Simulated transient in nested call");
                    }
                }, _ => Task.FromResult(false), cancellationToken: ct);

                return true;
            }, _ => Task.FromResult(false));

            // Assert
            TestContext.WriteLine($"Total attempts - Outer: {outerAttemptCount}, Inner: {innerAttemptCount}");

            // Если баг исправлен:
            // 1. Корневая стратегия сделала 2 попытки (одна упала в середине, вторая прошла целиком).
            NUnit.Framework.Assert.That(outerAttemptCount, Is.EqualTo(2), "Root strategy must retry the entire block");

            // 2. Вложенный блок вызывался ровно столько же раз, сколько внешний.
            // Это доказывает, что вложенная стратегия БЫЛА ПРОИГНОРИРОВАНА.
            NUnit.Framework.Assert.That(innerAttemptCount, Is.EqualTo(2), "Nested strategy must NOT retry independently");

            // Проверяем, что в базе всё корректно
            await using var verifyCtx = new TestDbContext(DbName);
            NUnit.Framework.Assert.That(await verifyCtx.Users.AnyAsync(x => x.Name == outerName), Is.True);
            NUnit.Framework.Assert.That(await verifyCtx.Users.AnyAsync(x => x.Name == innerName), Is.True);
        }
        finally
        {
            TestDbContext.IsRetryStrategy = false;
        }
    }

    [Test]
    public async Task Nested_DifferentInstance_MustThrowException()
    {
        // Сценарий:
        //   Outer ExecuteInTransactionAsync на контексте A.
        //   Внутри — новый scope, резолвим контекст B (новый инстанс того же типа).
        //   Inner ExecuteInTransactionAsync на B.
        //
        // Ожидание: ExecuteInTransactionAsync должен выбросить InvalidOperationException,
        // так как он обнаружит через AsyncLocal, что уже есть активная транзакция в другом контексте.
        var sp = InitServiceCollection().BuildServiceProvider();
        var outerCtx = sp.GetRequiredService<TestDbContext>();

        var outerId = Guid.NewGuid();
        var innerId = Guid.NewGuid();

        await outerCtx.ExecuteInTransactionAsync(async ct =>
        {
            outerCtx.Users.Add(new TestUser { Id = outerId, Name = "Outer_" + outerId, Years = 30 });
            await outerCtx.SaveChangesAsync(ct);

            using var innerScope = sp.CreateScope();
            var innerCtx = innerScope.ServiceProvider.GetRequiredService<TestDbContext>();

            TestContext.WriteLine($"Same instance? {ReferenceEquals(outerCtx, innerCtx)}");
            TestContext.WriteLine($"Inner CurrentTransaction is null? {innerCtx.Database.CurrentTransaction is null}");

            var ex = NUnit.Framework.Assert.ThrowsAsync<InvalidOperationException>((Func<Task>)(async () =>
            {
                await innerCtx.ExecuteInTransactionAsync(async ctInner =>
                {
                    innerCtx.Users.Add(new TestUser { Id = innerId, Name = "Inner_" + innerId, Years = 20 });
                    await innerCtx.SaveChangesAsync(ctInner);
                }, _ => Task.FromResult(false), cancellationToken: ct);
            }));

            NUnit.Framework.Assert.That(ex!.Message, Does.Contain("Detected a nested call to ExecuteInTransactionAsync using a different DbContext instance"));
        }, _ => Task.FromResult(false));

        // Читаем из свежего контекста, чтобы не увидеть кэш.
        await using var verify = new TestDbContext(DbName);
        var outerExists = await verify.Users.AnyAsync(x => x.Id == outerId);
        var innerExists = await verify.Users.AnyAsync(x => x.Id == innerId);

        TestContext.WriteLine($"Outer in DB: {outerExists}, Inner in DB: {innerExists}");

        NUnit.Framework.Assert.Multiple((Action)(() =>
        {
            NUnit.Framework.Assert.That(outerExists, Is.True, "Outer должен быть успешно закомичен");
            NUnit.Framework.Assert.That(innerExists, Is.False, "Inner не должен был создаться");
        }));
    }

    [Test]
    public async Task Nested_DifferentInstance_Scoped_ResolvesToSameInstance_WithinRootSp_Test()
    {
        // Вспомогательный тест, чтобы зафиксировать поведение DI:
        // при BuildServiceProvider() без явного CreateScope() Scoped-сервис резолвится из root-scope
        // и между двумя вызовами GetRequiredService вернётся ОДИН И ТОТ ЖЕ инстанс.
        // Это объясняет, почему существующие nested-тесты проходят.
        var sp = InitServiceCollection().BuildServiceProvider();
        var a = sp.GetRequiredService<TestDbContext>();
        var b = sp.GetRequiredService<TestDbContext>();

        NUnit.Framework.Assert.That(ReferenceEquals(a, b), Is.True, "Ожидается, что в root-scope Scoped сервис — singleton de facto");

        // А при явном новом scope — уже разные инстансы.
        using var scope = sp.CreateScope();
        var c = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        NUnit.Framework.Assert.That(ReferenceEquals(a, c), Is.False, "В новом scope должен быть новый инстанс");

        // Теперь проверим, что вложенный вызов через тот же инстанс РАБОТАЕТ
        await a.ExecuteInTransactionAsync(async ct =>
        {
            await b.ExecuteInTransactionAsync(async _ => { await Task.CompletedTask; }, _ => Task.FromResult(true), cancellationToken: ct);
            return true;
        }, _ => Task.FromResult(true));
    }

    [Test]
    public async Task Nested_TransientRetry_NoDuplicates_When_InnerStrategyIsBypassed_Test()
    {
        // Тест для проверки БЛОКЕРА: транзиентная ошибка во вложенном вызове НЕ должна
        // приводить к повторным попыткам внутри вложенного вызова.
        // Все повторы должны идти через корневую стратегию.

        var outerAttemptCount = 0;
        var innerAttemptCount = 0;
        var isInnerFailed = false;

        // ВАЖНО: Включаем стратегию ретраев ДО создания контекста
        TestDbContext.IsRetryStrategy = true;
        try
        {
            var sp = InitServiceCollection().BuildServiceProvider();
            var context = sp.GetRequiredService<TestDbContext>();

            await context.ExecuteInTransactionAsync(async ct =>
            {
                outerAttemptCount++;
                context.Users.Add(new TestUser { Name = "Outer_" + outerAttemptCount });
                await context.SaveChangesAsync(ct);

                await context.ExecuteInTransactionAsync(async ctInner =>
                {
                    innerAttemptCount++;

                    if (!isInnerFailed)
                    {
                        isInnerFailed = true;
                        // Симулируем транзиентную ошибку во вложенном вызове
                        throw new TimeoutException("Nested transient error");
                    }

                    context.Users.Add(new TestUser { Name = "Inner_" + innerAttemptCount });
                    await context.SaveChangesAsync(ctInner);
                }, _ => Task.FromResult(false), cancellationToken: ct);

                return true;
            }, _ => Task.FromResult(false));

            // Проверяем:
            // 1. Корневая стратегия сделала 2 попытки (одна упала, вторая прошла).
            NUnit.Framework.Assert.That(outerAttemptCount, Is.EqualTo(2), "Outer strategy must retry the whole block");
            // 2. Вложенная стратегия НЕ должна была делать ретрай сама.
            NUnit.Framework.Assert.That(innerAttemptCount, Is.EqualTo(2), "Inner strategy must NOT retry independently");
        }
        finally
        {
            TestDbContext.IsRetryStrategy = false;
        }
    }

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

        await masterContext.ExecuteInTransactionAsync(async ct =>
        {
            masterContext.Users.Add(new TestUser { Name = "MasterUser" });
            await masterContext.SaveChangesAsync(ct);

            // Попытка обернуть работу с репликой в ExecuteInTransactionAsync ДОЛЖНА выбросить исключение.
            var ex = NUnit.Framework.Assert.ThrowsAsync<InvalidOperationException>((Func<Task>)(async () =>
            {
                await using var replicaContext = new TestDbContext(DbName);
                await replicaContext.ExecuteInTransactionAsync(async ctReplica => { _ = await replicaContext.Users.AnyAsync(ctReplica); },
                    _ => Task.FromResult(false),
                    cancellationToken: ct);
            }));

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

    [Test]
    public async Task Nested_Failure_Should_Kill_Entire_Transaction_And_Retry_From_Root()
    {
        // Сценарий:
        // 1. Корень вставляет User1.
        // 2. Вложенный вызов вставляет User2, но потом кидает Transient Exception.
        // 3. Ожидаем: Весь блок перезапускается. User1 НЕ должен появиться в базе дважды (так как был откат).
        // 4. На второй попытке всё проходит успешно.

        TestDbContext.IsRetryStrategy = true;
        try
        {
            var sp = InitServiceCollection().BuildServiceProvider();
            var context = sp.GetRequiredService<TestDbContext>();

            var rootAttempt = 0;
            var nestedAttempt = 0;
            var userId1 = Guid.NewGuid();
            var userId2 = Guid.NewGuid();

            await context.ExecuteInTransactionAsync(
                async ct =>
                {
                    rootAttempt++;
                    TestContext.WriteLine($"Root attempt #{rootAttempt}");

                    // Вставляем первого пользователя
                    context.Users.Add(new TestUser { Id = userId1, Name = "RootUser", Years = 10 });
                    await context.SaveChangesAsync(ct);

                    await context.ExecuteInTransactionAsync(async ctInner =>
                    {
                        nestedAttempt++;
                        TestContext.WriteLine($"Nested attempt #{nestedAttempt}");

                        context.Users.Add(new TestUser { Id = userId2, Name = "NestedUser", Years = 20 });
                        await context.SaveChangesAsync(ctInner);

                        if (nestedAttempt == 1)
                        {
                            TestContext.WriteLine("Simulating nested failure...");
                            throw new TimeoutException("Simulated nested failure");
                        }
                    }, _ => Task.FromResult(false), cancellationToken: ct);

                    return true;
                },
                _ => Task.FromResult(false),
                options: new EfTransactionOptions { ClearChangeTrackerOnRetry = true });

            Assert.AreEqual(2, rootAttempt);
            Assert.AreEqual(2, nestedAttempt);

            // Проверяем что в базе ровно по одному пользователю (откат сработал)
            await using var verifyCtx = new TestDbContext(DbName);
            Assert.IsTrue(await verifyCtx.Users.AnyAsync(x => x.Id == userId1));
            Assert.IsTrue(await verifyCtx.Users.AnyAsync(x => x.Id == userId2));
            Assert.AreEqual(2, await verifyCtx.Users.CountAsync(x => x.Id == userId1 || x.Id == userId2));
        }
        finally
        {
            TestDbContext.IsRetryStrategy = false;
        }
    }

    [Test]
    public async Task Nested_DeepStack_Should_AllJoin_And_Rollback_On_Failure()
    {
        var sp = InitServiceCollection().BuildServiceProvider();
        var context = sp.GetRequiredService<TestDbContext>();
        var userId = Guid.NewGuid();

        try
        {
            await context.ExecuteInTransactionAsync(async ct1 =>
            {
                await context.Users.AddAsync(new TestUser { Id = userId, Name = "Level1" }, ct1);

                await context.ExecuteInTransactionAsync(async ct2 =>
                {
                    // No changes here, just nesting
                    await context.ExecuteInTransactionAsync(_ =>
                    {
                        try
                        {
                            // Failure at the deepest level
                            throw new InvalidOperationException("Deep failure");
                        }
                        catch (Exception exception)
                        {
                            return Task.FromException(exception);
                        }
                    }, _ => Task.FromResult(false), cancellationToken: ct2);
                    return true;
                }, _ => Task.FromResult(false), cancellationToken: ct1);
                return true;
            }, _ => Task.FromResult(false));
        }
        catch (InvalidOperationException ex) when (ex.Message == "Deep failure")
        {
            // Expected
        }

        // Verify: Level1 user should NOT be in DB
        await using var verifyCtx = new TestDbContext(DbName);
        Assert.IsFalse(await verifyCtx.Users.AnyAsync(x => x.Id == userId));
    }

    [Test]
    public async Task Nested_ManualSaveChangesAsync_DoesNotCommit_PhysicalTransaction()
    {
        var sp = InitServiceCollection().BuildServiceProvider();
        var context = sp.GetRequiredService<TestDbContext>();
        var userId = Guid.NewGuid();

        try
        {
            await context.ExecuteInTransactionAsync(async ct =>
            {
                // Nested call with manual SaveChanges
                await context.ExecuteInTransactionAsync(async ctInner =>
                {
                    await context.Users.AddAsync(new TestUser { Id = userId, Name = "NestedUser" }, ctInner);
                    await context.SaveChangesAsync(ctInner); // This flushes to DB but shouldn't COMMIT
                }, _ => Task.FromResult(false), cancellationToken: ct);

                // Root failure after nested "save"
                throw new Exception("Root failure");
            }, _ => Task.FromResult(false));
        }
        catch (Exception ex) when (ex.Message == "Root failure")
        {
            // Expected
        }

        // Verify: NestedUser should NOT be in DB because root transaction rolled back
        await using var verifyCtx = new TestDbContext(DbName);
        Assert.IsFalse(await verifyCtx.Users.AnyAsync(x => x.Id == userId));
    }

    [Test]
    public async Task Retry_Should_Provide_Fresh_ChangeTracker_When_Cleared()
    {
        TestDbContext.IsRetryStrategy = true;
        try
        {
            var sp = InitServiceCollection().BuildServiceProvider();
            var context = sp.GetRequiredService<TestDbContext>();

            var attempt = 0;
            var userId = Guid.NewGuid();

            await context.ExecuteInTransactionAsync(async ct =>
            {
                attempt++;

                if (attempt > 1)
                {
                    // On second attempt, ChangeTracker should be empty if ClearChangeTrackerOnRetry is true
                    Assert.IsFalse(context.ChangeTracker.Entries<TestUser>().Any(), "ChangeTracker must be cleared on retry");
                }

                // Track an entity
                context.Users.Add(new TestUser { Id = userId, Name = "RetryUser" });

                if (attempt == 1)
                {
                    // Verify entity is tracked
                    Assert.IsTrue(context.ChangeTracker.Entries<TestUser>().Any(e => e.Entity.Id == userId));
                    throw new TimeoutException("Simulated retry");
                }

                // Re-add and succeed (already added above)
                await context.SaveChangesAsync(ct);
            }, _ => Task.FromResult(false), new EfTransactionOptions { ClearChangeTrackerOnRetry = true });

            Assert.AreEqual(2, attempt);
        }
        finally
        {
            TestDbContext.IsRetryStrategy = false;
        }
    }
}