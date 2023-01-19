using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.Ef.Tests.Model;
using Dex.Cap.Ef.Tests.Strategies;
using Dex.Cap.OnceExecutor;
using Dex.Cap.OnceExecutor.Ef.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dex.Cap.Ef.Tests.OnceExecutorTests
{
    public class StrategyOnceExecutorTests : BaseTest
    {
        [Test]
        public async Task ExecuteAsync_DoubleCallExecuteAsync_ModificatorCalledOnce()
        {
            var sp = InitServiceCollection()
                .AddStrategyOnceExecutor<string, string, IConcrete1ExecutionStrategy, Concrete1ExecutionStrategy, TestDbContext>()
                .BuildServiceProvider();

            var arg = "StrategyOnceExecuteTest1";
            var executor = sp.GetRequiredService<IStrategyOnceExecutor<string, string, IConcrete1ExecutionStrategy>>();

            var firstResult = await executor.ExecuteAsync(arg, CancellationToken.None);
            var secondResult = await executor.ExecuteAsync(arg, CancellationToken.None);

            Assert.IsNotNull(firstResult);
            Assert.AreEqual(arg, firstResult);

            Assert.IsNotNull(secondResult);
            Assert.AreEqual(arg, secondResult);
        }

        [Test]
        public async Task ExecuteAsync_StrategyMultiplyRegistration_ReturnsValue()
        {
            var sp = InitServiceCollection()
                .AddStrategyOnceExecutor<string, string, IConcrete1ExecutionStrategy, Concrete1ExecutionStrategy, TestDbContext>()
                .AddStrategyOnceExecutor<string, string, IConcrete2ExecutionStrategy, Concrete2ExecutionStrategy, TestDbContext>()
                .AddStrategyOnceExecutor<string, TestUser, IConcrete3ExecutionStrategy, Concrete3ExecutionStrategy, TestDbContext>()
                .BuildServiceProvider();

            var arg1 = "StrategyOnceExecuteTest1";
            var arg2 = "StrategyOnceExecuteTest2";
            var arg3 = "StrategyOnceExecuteTest3";

            var executor1 = sp.GetRequiredService<IStrategyOnceExecutor<string, string, IConcrete1ExecutionStrategy>>();
            var executor2 = sp.GetRequiredService<IStrategyOnceExecutor<string, string, IConcrete2ExecutionStrategy>>();
            var executor3 = sp.GetRequiredService<IStrategyOnceExecutor<string, TestUser, IConcrete3ExecutionStrategy>>();

            var executor1Result = await executor1.ExecuteAsync(arg1, CancellationToken.None);
            var executor2Result = await executor2.ExecuteAsync(arg2, CancellationToken.None);
            var executor3Result = await executor3.ExecuteAsync(arg3, CancellationToken.None);

            Assert.IsNotNull(executor1Result);
            Assert.AreEqual(arg1, executor1Result);

            Assert.IsNotNull(executor2Result);
            Assert.AreEqual(arg2, executor2Result);

            Assert.IsNotNull(executor3Result);
            Assert.AreEqual(arg3, executor3Result!.Name);
        }
    }
}