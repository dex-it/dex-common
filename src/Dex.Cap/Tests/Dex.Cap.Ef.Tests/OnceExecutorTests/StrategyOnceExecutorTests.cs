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
                .AddStrategyOnceExecutor<Concrete1ExecutionStrategyRequest, string, Concrete1ExecutionStrategy, TestDbContext>()
                .BuildServiceProvider();

            var arg = new Concrete1ExecutionStrategyRequest { Value = "StrategyOnceExecuteTest1" };
            var executor = sp.GetRequiredService<IStrategyOnceExecutor<Concrete1ExecutionStrategyRequest, string>>();

            var firstResult = await executor.ExecuteAsync(arg, CancellationToken.None);
            var secondResult = await executor.ExecuteAsync(arg, CancellationToken.None);

            Assert.IsNotNull(firstResult);
            Assert.AreEqual(arg.Value, firstResult);

            Assert.IsNotNull(secondResult);
            Assert.AreEqual(arg.Value, secondResult);
        }

        [Test]
        public async Task ExecuteAsync_StrategyMultiplyRegistration_ReturnsValue()
        {
            var sp = InitServiceCollection()
                .AddStrategyOnceExecutor<Concrete1ExecutionStrategyRequest, string, Concrete1ExecutionStrategy, TestDbContext>()
                .AddStrategyOnceExecutor<Concrete2ExecutionStrategyRequest, string, Concrete2ExecutionStrategy, TestDbContext>()
                .AddStrategyOnceExecutor<Concrete3ExecutionStrategyRequest, TestUser, Concrete3ExecutionStrategy, TestDbContext>()
                .BuildServiceProvider();

            var arg1 = new Concrete1ExecutionStrategyRequest { Value = "StrategyOnceExecuteTest1" };
            var arg2 = new Concrete2ExecutionStrategyRequest { Value = "StrategyOnceExecuteTest2" };
            var arg3 = new Concrete3ExecutionStrategyRequest { Value = "StrategyOnceExecuteTest3" };

            var executor1 = sp.GetRequiredService<IStrategyOnceExecutor<Concrete1ExecutionStrategyRequest, string>>();
            var executor2 = sp.GetRequiredService<IStrategyOnceExecutor<Concrete2ExecutionStrategyRequest, string>>();
            var executor3 = sp.GetRequiredService<IStrategyOnceExecutor<Concrete3ExecutionStrategyRequest, TestUser>>();

            var executor1Result = await executor1.ExecuteAsync(arg1, CancellationToken.None);
            var executor2Result = await executor2.ExecuteAsync(arg2, CancellationToken.None);
            var executor3Result = await executor3.ExecuteAsync(arg3, CancellationToken.None);

            Assert.IsNotNull(executor1Result);
            Assert.AreEqual(arg1.Value, executor1Result);

            Assert.IsNotNull(executor2Result);
            Assert.AreEqual(arg2.Value, executor2Result);

            Assert.IsNotNull(executor3Result);
            Assert.AreEqual(arg3.Value, executor3Result!.Name);
        }
    }
}