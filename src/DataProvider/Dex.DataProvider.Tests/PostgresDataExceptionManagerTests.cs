using System;
using AutoFixture;
using Dex.DataProvider.Exceptions;
using Dex.DataProvider.Postgres;
using FluentAssertions;
using Npgsql;
using NUnit.Framework;

namespace Dex.DataProvider.Tests
{
    public class PostgresDataExceptionManagerTests
    {
        public PostgresDataExceptionManagerTests()
        {
            _fixture = new Fixture();
            _postgresDataExceptionManager = _fixture.Create<PostgresDataExceptionManager>();
        }

        [Test]
        public void NormalizeArgumentNullExceptionTest()
        {
            Assert.Throws<ArgumentNullException>(() => _postgresDataExceptionManager.Normalize(null!));
        }

        [Test]
        public void NormalizeNotPostgresInnerExceptionTest()
        {
            var exception = _fixture.Create<Exception>();

            var result = _postgresDataExceptionManager.Normalize(exception);

            result.Should().NotBeNull();
            result.GetType().Should().Be(typeof(DataProviderException));
            result.InnerException.Should().Be(exception);
            result.Message.Should().Be(exception.Message);
        }

        [TestCase(PostgresErrorCodes.ForeignKeyViolation, typeof(ForeignKeyViolationException))]
        [TestCase(PostgresErrorCodes.UniqueViolation, typeof(ObjectAlreadyExistsException))]
        [TestCase(PostgresErrorCodes.SerializationFailure, typeof(ConcurrentModifyException))]
        [TestCase(PostgresErrorCodes.Warning, typeof(DataProviderException))]
        public void NormalizePostgresInnerExceptionTest(string sqlState, Type resultExceptionType)
        {
            var postgresException = CreatePostgresException(sqlState);
            var exception = new Exception(postgresException.Message, postgresException);

            var result = _postgresDataExceptionManager.Normalize(exception);

            result.Should().NotBeNull();
            result.GetType().Should().Be(resultExceptionType);
            result.InnerException.Should().Be(exception);
            result.Message.Should().Be(postgresException.Message + postgresException.Detail);
        }
        
        [Test]
        public void IsRepeatArgumentNullExceptionTest()
        {
            Assert.Throws<ArgumentNullException>(() => _postgresDataExceptionManager.IsRepeatableException(null!));
        }

        [TestCase(typeof(Exception), false)]
        [TestCase(typeof(ForeignKeyViolationException), false)]
        [TestCase(typeof(ObjectAlreadyExistsException), false)]
        [TestCase(typeof(AccessModifyException), false)]
        [TestCase(typeof(DataProviderException), false)]
        [TestCase(typeof(ConcurrentModifyException), true)]
        public void IsRepeatActionTest(Type exceptionType, bool result)
        {
            var exception = (Exception) Activator.CreateInstance(exceptionType)!;

            var isRepeatAction = _postgresDataExceptionManager.IsRepeatableException(exception!);

            isRepeatAction.Should().Be(result);
        }

        [TestCase(PostgresErrorCodes.ForeignKeyViolation, false)]
        [TestCase(PostgresErrorCodes.UniqueViolation, false)]
        [TestCase(PostgresErrorCodes.SerializationFailure, true)]
        public void IsRepeatActionPostgresInnerExceptionTest(string sqlState, bool result)
        {
            var postgresException = CreatePostgresException(sqlState);
            var exception = new Exception(postgresException.Message, postgresException);

            var isRepeatAction = _postgresDataExceptionManager.IsRepeatableException(exception);

            isRepeatAction.Should().Be(result);
        }

        private PostgresException CreatePostgresException(string sqlState)
        {
            var postgresException = new PostgresException(
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                _fixture.Create<string>(),
                sqlState);
            return postgresException;
        }

        private readonly PostgresDataExceptionManager _postgresDataExceptionManager;
        private readonly Fixture _fixture;
    }
}