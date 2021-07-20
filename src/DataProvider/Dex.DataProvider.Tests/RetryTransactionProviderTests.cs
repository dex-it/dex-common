using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using AutoFixture;
using Dex.DataProvider.Contracts;
using Dex.DataProvider.Providers;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Dex.DataProvider.Tests
{
    public class RetryTransactionProviderTests
    {
        [SetUp]
        public void Init()
        {
            _fixture = new Fixture();

            _dataExceptionManagerMock = new Mock<IDataExceptionManager>(MockBehavior.Strict);
            _fixture.Inject(_dataExceptionManagerMock.Object);

            _retryTransactionSettingsMock = new Mock<IRetryTransactionSettings>(MockBehavior.Strict);
            _fixture.Inject(_retryTransactionSettingsMock.Object);

            _dataTransactionProviderMock = new Mock<IDataTransactionProvider>(MockBehavior.Strict);
            _fixture.Inject(_dataTransactionProviderMock.Object);

            _retryTransactionProvider = _fixture.Create<RetryTransactionProvider>();
        }

        [Test]
        public void Execute_ArgumentNullException_Test()
        {
            var provider = _dataTransactionProviderMock.Object;
            var arg = _fixture.Create<object>();

            // 1
            Assert.ThrowsAsync<ArgumentNullException>(
                () => _retryTransactionProvider.Execute(
                    null!,
                    _fixture.Create<Func<object, CancellationToken, Task<object>>>(),
                    arg));
            Assert.ThrowsAsync<ArgumentNullException>(
                () => _retryTransactionProvider.Execute(
                    provider,
                    ((Func<object, CancellationToken, Task<object>>) null)!,
                    arg));
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => _retryTransactionProvider.Execute(
                    provider,
                    _fixture.Create<Func<object, CancellationToken, Task<object>>>(),
                    arg,
                    _fixture.Create<IsolationLevel>(),
                    0));

            // 2
            Assert.ThrowsAsync<ArgumentNullException>(
                () => _retryTransactionProvider.Execute(
                    null!,
                    _fixture.Create<Func<CancellationToken, Task<object>>>()));
            Assert.ThrowsAsync<ArgumentNullException>(
                () => _retryTransactionProvider.Execute(
                    provider,
                    ((Func<CancellationToken, Task<object>>) null)!));
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => _retryTransactionProvider.Execute(
                    provider,
                    _fixture.Create<Func<CancellationToken, Task<object>>>(),
                    _fixture.Create<IsolationLevel>(),
                    0));

            // 3
            Assert.ThrowsAsync<ArgumentNullException>(
                () => _retryTransactionProvider.Execute(
                    null!,
                    _fixture.Create<Func<object, CancellationToken, Task>>(),
                    arg));
            Assert.ThrowsAsync<ArgumentNullException>(
                () => _retryTransactionProvider.Execute(provider, null!, arg));
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => _retryTransactionProvider.Execute(
                    provider,
                    _fixture.Create<Func<object, CancellationToken, Task>>(),
                    arg,
                    _fixture.Create<IsolationLevel>(),
                    0));

            // 4
            Assert.ThrowsAsync<ArgumentNullException>(
                () => _retryTransactionProvider.Execute(null!, _fixture.Create<Func<CancellationToken, Task>>()));
            Assert.ThrowsAsync<ArgumentNullException>(
                () => _retryTransactionProvider.Execute(provider, null!));
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => _retryTransactionProvider.Execute(
                    provider,
                    _fixture.Create<Func<CancellationToken, Task>>(),
                    _fixture.Create<IsolationLevel>(),
                    0));

            VerifyNormalize();
            VerifyIsRepeatableException(Times.Never());
            VerifyGetRetryDelay(Times.Never());
            VerifyTransaction(Times.Never());
            VerifyTransactionIsolationLevel(Times.Never());
        }

        [Test]
        public async Task Execute_Complete_Test()
        {
            var arg = _fixture.Create<object>();
            var level = _fixture.Create<IsolationLevel>();
            var transaction = SetupTransaction(level);
            transaction.Setup(self => self.Dispose());
            transaction.Setup(self => self.Complete());

            var result = await _retryTransactionProvider.Execute(
                _dataTransactionProviderMock.Object,
                ExecuteFunction,
                arg,
                level);

            result.Should().NotBeNull();
            result.Should().Be(arg);
            transaction.Verify(self => self.Dispose(), Times.Once);
            transaction.Verify(self => self.Complete(), Times.Once);
            VerifyTransaction(level, Times.Once());
            VerifyGetRetryDelay(Times.Never());
            VerifyIsRepeatableException(Times.Never());
            VerifyNormalize();
        }

        [Test]
        public async Task Execute_Complete_WrapperTaskT_Test()
        {
            var arg = _fixture.Create<object>();
            var level = _fixture.Create<IsolationLevel>();
            var transaction = SetupTransaction(level);
            transaction.Setup(self => self.Dispose());
            transaction.Setup(self => self.Complete());

            var result = await _retryTransactionProvider.Execute(
                _dataTransactionProviderMock.Object,
                _ => Task.FromResult(arg),
                level);

            result.Should().NotBeNull();
            result.Should().Be(arg);
            transaction.Verify(self => self.Dispose(), Times.Once);
            transaction.Verify(self => self.Complete(), Times.Once);
            VerifyTransaction(level, Times.Once());
            VerifyGetRetryDelay(Times.Never());
            VerifyIsRepeatableException(Times.Never());
            VerifyNormalize();
        }
        
        [Test]
        public async Task Execute_Complete_WrapperTaskArg_Test()
        {
            var level = _fixture.Create<IsolationLevel>();
            var transaction = SetupTransaction(level);
            transaction.Setup(self => self.Dispose());
            transaction.Setup(self => self.Complete());

            await _retryTransactionProvider.Execute(
                _dataTransactionProviderMock.Object,
                (_, _) => Task.CompletedTask,
                _fixture.Create<object>(),
                level);

            transaction.Verify(self => self.Dispose(), Times.Once);
            transaction.Verify(self => self.Complete(), Times.Once);
            VerifyTransaction(level, Times.Once());
            VerifyGetRetryDelay(Times.Never());
            VerifyIsRepeatableException(Times.Never());
            VerifyNormalize();
        }
        
        [Test]
        public async Task Execute_Complete_WrapperTask_Test()
        {
            var level = _fixture.Create<IsolationLevel>();
            var transaction = SetupTransaction(level);
            transaction.Setup(self => self.Dispose());
            transaction.Setup(self => self.Complete());

            await _retryTransactionProvider.Execute(
                _dataTransactionProviderMock.Object,
                _ => Task.CompletedTask,
                level);

            transaction.Verify(self => self.Dispose(), Times.Once);
            transaction.Verify(self => self.Complete(), Times.Once);
            VerifyTransaction(level, Times.Once());
            VerifyGetRetryDelay(Times.Never());
            VerifyIsRepeatableException(Times.Never());
            VerifyNormalize();
        }

        [Test]
        public void Execute_IsRepeatableExceptionFalse_Test()
        {
            var arg = _fixture.Create<object>();
            var level = _fixture.Create<IsolationLevel>();
            var transaction = SetupTransaction(level);
            transaction.Setup(self => self.Dispose());
            var exception = _fixture.Create<Exception>();
            transaction.Setup(self => self.Complete()).Throws(exception);
            SetupIsRepeatableException(exception, false);

            Assert.ThrowsAsync<Exception>(() => _retryTransactionProvider.Execute(
                _dataTransactionProviderMock.Object,
                ExecuteFunction,
                arg,
                level));

            transaction.Verify(self => self.Dispose(), Times.Once);
            transaction.Verify(self => self.Complete(), Times.Once);
            VerifyTransaction(level, Times.Once());
            VerifyGetRetryDelay(Times.Never());
            VerifyIsRepeatableException(exception, Times.Once());
            VerifyNormalize();
        }

        [Test]
        public void Execute_OunOfRetryCount_Test()
        {
            var arg = _fixture.Create<object>();
            var level = _fixture.Create<IsolationLevel>();
            var transaction = SetupTransaction(level);
            transaction.Setup(self => self.Dispose());
            var exception = _fixture.Create<Exception>();
            transaction.Setup(self => self.Complete()).Throws(exception);
            SetupIsRepeatableException(exception, true);
            SetupGetRetryDelay();

            Assert.ThrowsAsync<Exception>(() => _retryTransactionProvider.Execute(
                _dataTransactionProviderMock.Object,
                ExecuteFunction,
                arg,
                level,
                2));

            transaction.Verify(self => self.Dispose(), Times.Exactly(2));
            transaction.Verify(self => self.Complete(), Times.Exactly(2));
            VerifyTransaction(level, Times.Exactly(2));
            VerifyGetRetryDelay(Times.Once());
            VerifyIsRepeatableException(exception, Times.Exactly(2));
            VerifyNormalize();
        }

        private void SetupIsRepeatableException(Exception exception, bool result)
        {
            _dataExceptionManagerMock
                .Setup(self => self.IsRepeatableException(exception))
                .Returns(result);
        }

        private void VerifyIsRepeatableException(Times times)
        {
            _dataExceptionManagerMock
                .Verify(self => self.IsRepeatableException(It.IsAny<Exception>()), times);
        }

        private void VerifyIsRepeatableException(Exception exception, Times times)
        {
            _dataExceptionManagerMock
                .Verify(self => self.IsRepeatableException(exception), times);
        }

        private void VerifyNormalize()
        {
            _dataExceptionManagerMock
                .Verify(self => self.Normalize(It.IsAny<Exception>()), Times.Never);
        }

        private void SetupGetRetryDelay()
        {
            _retryTransactionSettingsMock
                .SetupGet(self => self.RetryDelay)
                .Returns(TimeSpan.Zero);
        }

        private void VerifyGetRetryDelay(Times times)
        {
            _retryTransactionSettingsMock
                .VerifyGet(self => self.RetryDelay, times);
        }

        private void VerifyTransaction(Times times)
        {
            _dataTransactionProviderMock
                .Verify(self => self.BeginTransaction(), times);
        }

        private void VerifyTransaction(IsolationLevel level, Times times)
        {
            _dataTransactionProviderMock
                .Verify(self => self.BeginTransaction(level), times);
        }

        private Mock<IDataTransaction> SetupTransaction(IsolationLevel level)
        {
            var mock = new Mock<IDataTransaction>(MockBehavior.Strict);

            _dataTransactionProviderMock
                .Setup(self => self.BeginTransaction(level))
                .Returns(mock.Object);

            return mock;
        }

        private void VerifyTransactionIsolationLevel(Times times)
        {
            _dataTransactionProviderMock
                .Verify(self => self.BeginTransaction(It.IsAny<IsolationLevel>()), times);
        }

        private Task<object> ExecuteFunction(object value, CancellationToken _)
        {
            return Task.FromResult(value);
        }

        private Fixture _fixture;
        private RetryTransactionProvider _retryTransactionProvider;
        private Mock<IDataExceptionManager> _dataExceptionManagerMock;
        private Mock<IRetryTransactionSettings> _retryTransactionSettingsMock;
        private Mock<IDataTransactionProvider> _dataTransactionProviderMock;
    }
}