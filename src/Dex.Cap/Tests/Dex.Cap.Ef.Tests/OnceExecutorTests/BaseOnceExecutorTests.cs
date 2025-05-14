using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.Common.Ef;
using Dex.Cap.Common.Ef.Extensions;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OnceExecutorTests
{
    public class BaseOnceExecutorTests : BaseTest
    {
        [Test]
        public async Task OnceExecute_AddEntity_SuccessTrue()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();
            var executor = sp.GetRequiredService<IOnceExecutor<IEfTransactionOptions, TestDbContext>>();

            var stepId = Guid.NewGuid().ToString("N");
            var user = new TestUser { Name = "OnceExecuteTest", Years = 18 };

            await executor.ExecuteAsync(stepId, async (context, token) =>
            {
                await context.Users.AddAsync(user, token);
                await context.SaveChangesAsync(token);
            });
            await dbContext.SaveChangesAsync();

            var xUser = await dbContext.FindAsync<TestUser>(user.Id);
            Assert.IsNotNull(xUser);
        }

        [Test]
        public void DoubleInsert_ThrowDbUpdateException()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();

            Assert.CatchAsync<DbUpdateException>(async () =>
            {
                var user = new TestUser { Name = "DoubleInsertTest", Years = 18 };

                await dbContext.Users.AddAsync(user);
                await dbContext.SaveChangesAsync();

                await dbContext.Users.AddAsync(user);
                await dbContext.SaveChangesAsync();
            });
        }

        [Test]
        public async Task DoubleInsertByOnceExecutor_SuccessTrue()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var executor = sp.GetRequiredService<IOnceExecutor<IEfTransactionOptions, TestDbContext>>();

            var stepId = Guid.NewGuid().ToString("N");
            var user = new TestUser { Name = "OnceExecuteTest", Years = 18 };

            var firstResult = await executor.ExecuteAsync(stepId, async (context, ct) =>
                {
                    await context.Users.AddAsync(user, ct);
                    await context.SaveChangesAsync(ct);
                },
                (context, ct) => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteTest", ct)
            );

            var secondResult = await executor.ExecuteAsync(stepId, async (context, ct) =>
                {
                    await context.Users.AddAsync(user, ct);
                    await context.SaveChangesAsync(ct);
                },
                (context, ct) => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteTest", ct)
            );

            Assert.IsNotNull(firstResult);
            Assert.AreEqual(user.Id, firstResult!.Id);

            Assert.IsNotNull(secondResult);
            Assert.AreEqual(user.Id, secondResult!.Id);
        }

        [Test]
        public void OnceExecuteThrowTransientException_TriggeringRetryStrategy_ThrowRetryLimitExceededException()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var executor = sp.GetRequiredService<IOnceExecutor<IEfTransactionOptions, TestDbContext>>();
            var stepId = Guid.NewGuid().ToString("N");

            TestUser? result = null;

            Assert.CatchAsync<RetryLimitExceededException>(async () =>
            {
                result = await executor.ExecuteAsync(stepId, async (context, ct) =>
                    {
                        var user = new TestUser { Name = "OnceExecuteTest", Years = 18 };
                        await context.Users.AddAsync(user, ct);
                        await context.SaveChangesAsync(ct);
                        await Task.Delay(TimeSpan.FromMilliseconds(50), ct);

                        throw new TimeoutException("The operation has timed out.");
                    },
                    (context, ct) => context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteTest", ct)
                );
            });

            Assert.IsNull(result);
        }

        [Test]
        public async Task ConcurrentOnceExecuteWithSameIdempotentKey_FirstSuccessTrue_OtherThrowDbUpdateException()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var stepId = Guid.NewGuid().ToString("N");
            const int taskCount = 5;
            var tasks = new List<Task>(taskCount);

            for (var i = 0; i < taskCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var scope = sp.CreateScope();
                    var executor = scope.ServiceProvider.GetRequiredService<IOnceExecutor<IEfTransactionOptions, TestDbContext>>();

                    await executor.ExecuteAsync(stepId, async (context, c) =>
                        {
                            await Task.Delay(1000, c);
                            var user = new TestUser { Name = "OnceExecuteTest", Years = 18 };
                            await context.Users.AddAsync(user, c);
                            await context.SaveChangesAsync(c);
                        }
                    );
                }));
            }

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch (AggregateException aggregateEx)
            {
                var uniqueViolationExceptions = aggregateEx.InnerExceptions
                    .Where(ex => ex is DbUpdateException
                    {
                        InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
                    }).ToArray();

                Assert.That(aggregateEx.InnerExceptions.Count, Is.EqualTo(uniqueViolationExceptions.Length),
                    "Все исключения должны быть связаны с нарушением уникальности ключа.");
                Assert.That(aggregateEx.InnerExceptions.Count, Is.EqualTo(taskCount - 1));
            }

            using (var scope = sp.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
                var users = await dbContext.Users.Where(u => u.Name == "OnceExecuteTest").ToListAsync();
                Assert.That(users.Count, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task OnceExecuteUnsavedChangesExceptionTest()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();
            var logger = sp.GetRequiredService<ILogger<BaseOnceExecutorTests>>();
            var executor = sp.GetRequiredService<IOnceExecutor<IEfTransactionOptions, TestDbContext>>();

            var stepId = Guid.NewGuid().ToString("N");
            var user = new TestUser { Name = "OnceExecuteTest", Years = 18 };

            try
            {
                await executor.ExecuteAsync(stepId,
                    (context, token) =>
                    {
                        return context.ExecuteInTransactionScopeAsync(
                            context, async (state, t) =>
                            {
                                await state.Users.AddAsync(user, t);
                                await state.SaveChangesAsync(t);
                            },
                            (_, _) => Task.FromResult(false),
                            cancellationToken: token);
                    });
                await dbContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception Data: {Data}", e.Data);
                throw;
            }
        }

        [Test]
        public async Task OnceExecuteBeginTransactionTest()
        {
            var sp = InitServiceCollection()
                .BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();
            var executor = sp.GetRequiredService<IOnceExecutor<IEfTransactionOptions, TestDbContext>>();
            var efOptions = new EfTransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted };

            var stepId = Guid.NewGuid().ToString("N");
            var user = new TestUser { Name = "OnceExecuteBeginTransactionTest", Years = 18 };

            // transaction 1
            var result = await executor.ExecuteAsync(stepId, async (context, token) =>
                {
                    await context.Users.AddAsync(user, token);
                    await context.SaveChangesAsync(token);
                },
                (context, token) =>
                    context.Users.FirstOrDefaultAsync(x => x.Name == "OnceExecuteBeginTransactionTest", token),
                efOptions,
                cancellationToken: CancellationToken.None
            );

            Assert.IsNotNull(result);
            Assert.AreEqual(user.Id, result!.Id);

            await dbContext.Users.AddAsync(new TestUser { Name = "OnceExecuteBeginTransactionTest-2" });
            // transaction 2
            await dbContext.SaveChangesAsync();

            await CheckUsers("OnceExecuteBeginTransactionTest", "OnceExecuteBeginTransactionTest-2");
        }

        private async Task CheckUsers(params string[] userNames)
        {
            await using var testDbContext = new TestDbContext(DbName);
            foreach (var u in userNames)
            {
                Assert.IsNotNull(await testDbContext.Users.SingleAsync(x => x.Name == u));
            }
        }
    }
}