using System;
using System.Transactions;
using Dex.DataProvider.Transactions;
using FluentAssertions;
using NUnit.Framework;

namespace Dex.DataProvider.Tests
{
    public class TransactionScopeToDataTransactionAdapterTests
    {
        [Test]
        public void Test()
        {
            var startLevel = Transaction.Current?.IsolationLevel;
            startLevel.Should().BeNull();

            var transaction = CreateTransaction();

            startLevel = Transaction.Current?.IsolationLevel;
            startLevel.Should().Be(IsolationLevel.ReadCommitted);

            transaction.Complete();

            Assert.Throws<InvalidOperationException>(() => _ = Transaction.Current?.IsolationLevel);

            transaction.Dispose();
            Assert.Throws<ObjectDisposedException>(() => transaction.Complete());
        }

        private static TransactionScopeToDataTransactionAdapter CreateTransaction()
        {
            var txOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted
            };

            var transactionScope = new TransactionScope(
                TransactionScopeOption.Required,
                txOptions,
                TransactionScopeAsyncFlowOption.Enabled);

            var transaction = new TransactionScopeToDataTransactionAdapter(transactionScope);
            return transaction;
        }
    }
}