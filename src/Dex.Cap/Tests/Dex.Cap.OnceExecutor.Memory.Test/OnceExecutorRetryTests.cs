using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.Ef.Tests;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.OnceExecutor.Memory.Test
{
    /// <summary>
    /// Для выполнения этих тестов нужно поставить заглушку в стратегии повтора, чтобы там падала трансиентная ошибка и выполнялись повторы
    /// </summary>
    public class OnceExecutorRetryTests : BaseTest
    {
        [Test]
        public async Task BasicTest()
        {
            var (executor, dbContext) = BuildServices();

            var stepId = Guid.NewGuid().ToString("N");
            var guid = Guid.NewGuid();

            Assert.ThrowsAsync<RetryLimitExceededException>(async () => await executor.ExecuteAndSaveInTransactionAsync(stepId,
                async (context, _) =>
                {
                    context.Add(new TestUser {Id = guid, Name = "BasicTest " + DateTime.Now, Years = 18});
                    await Task.CompletedTask;
                }));

            // Assertion
            Assert.That(await dbContext.Set<TestUser>().AnyAsync(u => u.Id == guid), Is.False);
        }

        [Test]
        public async Task BasicWithSelectTest()
        {
            var (executor, dbContext) = BuildServices();

            var stepId = Guid.NewGuid().ToString("N");
            var guid = Guid.NewGuid();

            Assert.ThrowsAsync<RetryLimitExceededException>(async () => await executor.ExecuteAndSaveInTransactionAsync(stepId,
                async (context, ct) =>
                {
                    var firstUser = await context.Set<TestUser>().FirstOrDefaultAsync(ct);

                    context.Add(new TestUser {Id = Guid.NewGuid(), Name = "BasicWithSelectTest2 " + DateTime.Now, Years = 18});
                    var lastUser = await context.Set<TestUser>().OrderBy(c => c.Name).LastAsync(ct);

                    context.Add(new TestUser {Id = guid, Name = "BasicWithSelectTest " + DateTime.Now, Years = 18});
                    await Task.CompletedTask;
                }));

            // Assertion
            Assert.That(await dbContext.Set<TestUser>().AnyAsync(u => u.Id == guid), Is.False);
        }

        [Test]
        public async Task BasicSaveTest()
        {
            var (executor, dbContext) = BuildServices();

            var stepId = Guid.NewGuid().ToString("N");
            var guid = Guid.NewGuid();

            await executor.ExecuteAndSaveInTransactionAsync(stepId,
                async (context, ct) =>
                {
                    context.Add(new TestUser {Id = guid, Name = "BasicSaveTest " + DateTime.Now, Years = 18});

                    // если здесь сохраняем, то это отдельная транзакция и последующие падения не имеют значение
                    // здесь так же сохраняется ключ идемпотентности поэтому тест не падает
                    await context.SaveChangesAsync(ct);
                });

            // Assertion
            Assert.That(await dbContext.Set<TestUser>().AnyAsync(u => u.Id == guid), Is.True);
        }

        [Test]
        public async Task BasicSaveTwiceTest()
        {
            var (executor, dbContext) = BuildServices();

            var stepId = Guid.NewGuid().ToString("N");
            var guid = Guid.NewGuid();

            await executor.ExecuteAndSaveInTransactionAsync(stepId,
                async (context, ct) =>
                {
                    context.Add(new TestUser {Id = guid, Name = "BasicSaveTwiceTest " + DateTime.Now, Years = 18});

                    // если здесь сохраняем, то это отдельная транзакция и последующие падения не имеют значение
                    await context.SaveChangesAsync(ct);

                    context.Add(new TestUser {Id = Guid.NewGuid(), Name = "BasicSaveTwiceTestSecondTest " + DateTime.Now, Years = 18});

                    // если здесь сохраняем, то это отдельная транзакция и последующие падения не имеют значение
                    await context.SaveChangesAsync(ct);
                });

            dbContext.Add(new TestUser {Id = Guid.NewGuid(), Name = "BasicSaveTwiceTestThirdTest " + DateTime.Now, Years = 18});
            // если здесь сохраняем, то это отдельная транзакция и последующие падения не имеют значение
            await dbContext.SaveChangesAsync(CancellationToken.None);

            // Assertion
            Assert.That(await dbContext.Set<TestUser>().AnyAsync(u => u.Id == guid), Is.True);
        }

        [Test]
        public async Task ModificatorExceptionTest()
        {
            var (executor, dbContext) = BuildServices();

            var stepId = Guid.NewGuid().ToString("N");
            var guid = Guid.NewGuid();

            var result = Assert.ThrowsAsync<TimeoutException>(async () =>
                await executor.ExecuteAndSaveInTransactionAsync(stepId,
                    async (context, _) =>
                    {
                        context.Add(new TestUser {Id = guid, Name = "ModificatorExceptionTest " + DateTime.Now, Years = 18});
                        throw new TimeoutException(nameof(ModificatorExceptionTest)); // ДО RETRY НЕ ДОЙДЕТ
                        await Task.CompletedTask;
                    }));

            // Assertion
            Assert.That(await dbContext.Set<TestUser>().AnyAsync(u => u.Id == guid), Is.False);
        }

        [Test]
        public async Task ModificatorExceptionAfterSaveTest()
        {
            var (executor, dbContext) = BuildServices();

            var stepId = Guid.NewGuid().ToString("N");
            var guid = Guid.NewGuid();

            Assert.ThrowsAsync<TimeoutException>(async () => await executor.ExecuteAndSaveInTransactionAsync(stepId,
                async (context, _) =>
                {
                    context.Add(new TestUser {Id = guid, Name = "ModificatorExceptionAfterSaveTest " + DateTime.Now, Years = 18});
                    await context.SaveChangesAsync(CancellationToken.None); // ЭТО БУДЕТ СОХРАНЕНО НЕ СМОТРЯ НА ПОСЛЕДУЮЩИЕ ОШИБКИ
                    throw new TimeoutException(nameof(ModificatorExceptionTest));
                    context.Add(new TestUser {Id = Guid.NewGuid(), Name = "OnceExecuteTest2 " + DateTime.Now, Years = 18});
                    await context.SaveChangesAsync(CancellationToken.None);
                }));

            // Assertion
            Assert.That(await dbContext.Set<TestUser>().AnyAsync(u => u.Id == guid), Is.True);
        }

        [Test]
        public async Task ExternalTransactionTest()
        {
            var (executor, dbContext) = BuildServices();

            var stepId = Guid.NewGuid().ToString("N");
            var guid = Guid.NewGuid();

            var strategy = dbContext.Database.CreateExecutionStrategy();
            var result = await strategy.ExecuteAsync(
                (guid, stepId, executor),
                async static (context, st, ct) =>
                {
                    var transactionOptions = new TransactionOptions
                    {
                        IsolationLevel = IsolationLevel.ReadCommitted,
                        Timeout = TimeSpan.FromSeconds(60)
                    };

                    context.ChangeTracker.Clear();

                    // открываем скоуп транзакции или привязываемся к существующему
                    using (var transactionScope =
                           new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
                    {
                        var firstUser = await context.Set<TestUser>().FirstOrDefaultAsync(ct);

                        context.Add(new TestUser {Id = st.guid, Name = "ExternalTransactionTest " + DateTime.Now, Years = 18});
                        var lastUser = await context.Set<TestUser>().OrderBy(c => c.Name).LastAsync(ct);
                        await context.SaveChangesAsync(CancellationToken.None);

                        //Assert.ThrowsAsync<TimeoutException>(
                        await st.executor.ExecuteAndSaveInTransactionAsync(st.stepId,
                            async (context, _) =>
                            {
                                context.Add(new TestUser {Id = Guid.NewGuid(), Name = "ExternalTransactionTest2 " + DateTime.Now, Years = 18});
                                //await context.SaveChangesAsync(CancellationToken.None);
                            });
                        //);

                        // Assertion
                        Assert.That(await context.Set<TestUser>().AnyAsync(u => u.Id == st.guid), Is.True);

                        transactionScope.Complete();
                        return st.guid;
                    }
                },
                async static (_, st, ct) => new ExecutionResult<Guid>(await _.Set<TestUser>().AsNoTracking().AnyAsync(u => u.Id == st.guid), st.guid),
                CancellationToken.None
            );
        }

        [Test]
        public async Task OnceExecuteRetryTest()
        {
            var (executor, dbContext) = BuildServices();

            var stepId = Guid.NewGuid().ToString("N");
            var guid = Guid.NewGuid();
            var user = new TestUser {Name = "OnceExecuteTest", Years = 18};
            // var user2 = new TestUser {Name = "OnceExecuteTest2", Years = 18};
            var counter = 3;

            /*await CreateUser(dbContext, user, CancellationToken.None);
            await dbContext.SaveChangesAsync();*/

            //var firstAsync = await dbContext.Users.FirstAsync(x=>x.Name == "OnceExecuteTest");

            await executor.ExecuteAndSaveInTransactionAsync(stepId, async (context, token) =>
            {
                context.Add(new TestUser {Id = guid, Name = "OnceExecuteTest", Years = 18});
                if (counter-- > 0)
                {
                    throw new TimeoutException();
                }

                await context.SaveChangesAsync(token);
            });

            //await dbContext.SaveChangesAsync();

            //var firstAsync2 = await dbContext.Users.FirstAsync(x=>x.Name == "OnceExecuteTestChange");
            //Assert.IsNotNull(firstAsync2);

            //var dbContext = sp.GetRequiredService<TestDbContext>();

            var xUser = await dbContext.FindAsync<TestUser>(user.Id);
            Assert.IsNotNull(xUser);

            ValueTask<EntityEntry<TestUser>> CreateUser(TestDbContext context, TestUser newUser, CancellationToken token)
            {
                return context.Users.AddAsync(newUser, token);
            }

            ValueTask<EntityEntry<TestUser>> CreateUserEx(TestDbContext context, TestUser newUser, CancellationToken token)
            {
                throw new TimeoutException();
            }
        }

        private (IOnceExecutor<IEfOptions, TestDbContext> executor, TestDbContext dbContext) BuildServices()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var executor = sp.GetRequiredService<IOnceExecutor<IEfOptions, TestDbContext>>();
            var dbContext = sp.GetRequiredService<TestDbContext>();
            return (executor, dbContext);
        }
    }
}