using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Ef.Tests.Strategies;
using Dex.Cap.OnceExecutor;
using Dex.Cap.OnceExecutor.Ef;
using Dex.Cap.OnceExecutor.Ef.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OnceExecutorTests
{
    public class StrategyOnceExecutorTests : BaseTest
    {
        [Test]
        public async Task StrategyOnceExecuteTest1()
        {
            var sp = InitServiceCollection()
                .AddStrategyOnceExecutor<string, string, TestExecutionStrategy1, TestDbContext>()
                /*.AddScoped<IStrategyOnceExecutor<string, string, TestExecutionStrategy1>, StrategyOnceExecutorEf<string, string, TestExecutionStrategy1, TestDbContext>>()
                .AddScoped<IOnceExecutionStrategy<string, string>, TestExecutionStrategy1>()*/
                .BuildServiceProvider();

            var arg = "StrategyOnceExecuteTest1";
            //var st = sp.GetRequiredService<IOnceExecutionStrategy<string, string>>();
            var executor = sp.GetRequiredService<IStrategyOnceExecutor<string, string>>();

            var firstResult = await executor.ExecuteAsync(arg, CancellationToken.None);
            var secondResult = await executor.ExecuteAsync(arg, CancellationToken.None);

            Assert.IsNotNull(firstResult);
            Assert.AreEqual(arg, firstResult);

            Assert.IsNotNull(secondResult);
            Assert.AreEqual(arg, secondResult);
        }

        [Test]
        public async Task StrategyOnceExecuteTest2()
        {
            var sp = InitServiceCollection()
                .AddStrategyOnceExecutor<string, string, TestExecutionStrategy1, TestDbContext>()
                .AddStrategyOnceExecutor<string, TestUser, TestExecutionStrategy2, TestDbContext>()
                .BuildServiceProvider();

            var arg1 = "StrategyOnceExecuteTest1";
            var arg2 = "StrategyOnceExecuteTest2";
            var executor1 = sp.GetRequiredService<IStrategyOnceExecutor<string, string>>();
            var executor2 = sp.GetRequiredService<IStrategyOnceExecutor<string, TestUser>>();

            var executor1FirstResult = await executor1.ExecuteAsync(arg1, CancellationToken.None);
            var executor1SecondResult = await executor1.ExecuteAsync(arg1, CancellationToken.None);

            var executor2FirstResult = await executor2.ExecuteAsync(arg2, CancellationToken.None);
            var executor2SecondResult = await executor2.ExecuteAsync(arg2, CancellationToken.None);

            Assert.IsNotNull(executor1FirstResult);
            Assert.AreEqual(arg1, executor1FirstResult);
            Assert.IsNotNull(executor1SecondResult);
            Assert.AreEqual(arg1, executor1SecondResult);

            Assert.IsNotNull(executor2FirstResult);
            Assert.AreEqual(arg2, executor2FirstResult!.Name);
            Assert.IsNotNull(executor2SecondResult);
            Assert.AreEqual(arg2, executor2SecondResult!.Name);
        }
    }
}