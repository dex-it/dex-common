using System;
using System.Threading;
using System.Threading.Tasks;
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