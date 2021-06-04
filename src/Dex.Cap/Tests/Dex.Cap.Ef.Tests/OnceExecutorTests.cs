using System;
using System.Linq;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.OnceExecutor.Ef;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests
{
    public class OnceExecutorTests : BaseTest
    {
        [Test]
        public void DoubleInsertTest()
        {
            Assert.CatchAsync<DbUpdateException>(async () =>
            {
                var user = new User {Name = "mmx003", Years = 18};
                await using (var testDbContext = new TestDbContext(DbName))
                {
                    await testDbContext.Users.AddAsync(user);
                    await testDbContext.SaveChangesAsync();
                }

                await using (var testDbContext = new TestDbContext(DbName))
                {
                    await testDbContext.Users.AddAsync(user);
                    await testDbContext.SaveChangesAsync();
                }
            });
        }

        [Test]
        public async Task OnceExecuteTest()
        {
            var stepId = Guid.NewGuid();
            var user = new User {Name = "mmx003", Years = 18};

            await using (var testDbContext = new TestDbContext(DbName))
            {
                var ex = new OnceExecutorEntityFramework<TestDbContext, User>(testDbContext);

                var result = await ex.Execute(stepId,
                    context => context.Users.AddAsync(user).AsTask(),
                    context => context.Users.FirstOrDefaultAsync(x => x.Name == "mmx003")
                );

                Assert.AreEqual(user.Id, result.Id);
            }

            await using (var testDbContext = new TestDbContext(DbName))
            {
                var ex = new OnceExecutorEntityFramework<TestDbContext, User>(testDbContext);

                var result = await ex.Execute(stepId,
                    context => context.Users.AddAsync(user).AsTask(),
                    context => context.Users.FirstOrDefaultAsync(x => x.Name == "mmx003")
                );

                Assert.AreEqual(user.Id, result.Id);
            }
        }
    }
}