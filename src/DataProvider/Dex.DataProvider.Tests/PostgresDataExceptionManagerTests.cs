using System;
using AutoFixture;
using Dex.DataProvider.Exceptions;
using Dex.DataProvider.Postgres;
using Dex.DataProvider.Tests.Helpers;
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
        public void Normalize_ArgumentNullException_Test()
        {
            Assert.Throws<ArgumentNullException>(() => _postgresDataExceptionManager.Normalize(null!));
        }

        [Test]
        public void Normalize_NotPostgresInnerException_Test()
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
        public void Normalize_NormalizePostgresExceptionReturnNotNull_Test(string sqlState, Type resultExceptionType)
        {
            var postgresException = CreatePostgresException(sqlState);
            var exception = CreateException(postgresException);

            var result = _postgresDataExceptionManager.Normalize(exception);

            result.Should().NotBeNull();
            result.GetType().Should().Be(resultExceptionType);
            result.InnerException.Should().Be(exception);
            result.Message.Should().Be(postgresException.Message + postgresException.Detail);
        }

        [Test]
        public void Normalize_NormalizePostgresExceptionReturnNull_Test()
        {
            var postgresException = CreatePostgresException(PostgresErrorCodes.Warning);
            var exception = CreateException(postgresException);

            var result = _postgresDataExceptionManager.Normalize(exception);

            result.Should().NotBeNull();
            result.GetType().Should().Be(typeof(DataProviderException));
            result.InnerException.Should().Be(exception);
            result.Message.Should().Be(exception.Message);
        }
        
        [Test]
        public void IsRepeatableException_ArgumentNullException_Test()
        {
            Assert.Throws<ArgumentNullException>(() => _postgresDataExceptionManager.IsRepeatableException(null!));
        }

        [TestCase(typeof(Exception), false)]
        [TestCase(typeof(ForeignKeyViolationException), false)]
        [TestCase(typeof(ObjectAlreadyExistsException), false)]
        [TestCase(typeof(AccessModifyException), false)]
        [TestCase(typeof(DataProviderException), false)]
        [TestCase(typeof(ConcurrentModifyException), true)]
        public void IsRepeatableException_СheckExceptionType_Test(Type exceptionType, bool result)
        {
            var exception = (Exception) Activator.CreateInstance(exceptionType)!;

            var isRepeatAction = _postgresDataExceptionManager.IsRepeatableException(exception!);

            isRepeatAction.Should().Be(result);
        }

        [TestCase(PostgresErrorCodes.ForeignKeyViolation, false)]
        [TestCase(PostgresErrorCodes.UniqueViolation, false)]
        [TestCase(PostgresErrorCodes.SerializationFailure, true)]
        public void IsRepeatableException_СheckSqlState_Test(string sqlState, bool result)
        {
            var postgresException = CreatePostgresException(sqlState);
            var exception = CreateException(postgresException);

            var isRepeatAction = _postgresDataExceptionManager.IsRepeatableException(exception);

            isRepeatAction.Should().Be(result);
        }

        [Test]
        public void NormalizePostgresException_ArgumentNullException_Test()
        {
            var exceptionManager = _fixture.Create<PostgresDataExceptionManagerTest>();
            
            Assert.Throws<ArgumentNullException>(
                () => exceptionManager.NormalizePostgresExceptionTest(null!, _fixture.Create<Exception>()));
            Assert.Throws<ArgumentNullException>(
                () => exceptionManager.NormalizePostgresExceptionTest(_fixture.Create<PostgresException>(), null!));

            exceptionManager.NormalizePostgresExceptionTest(
                _fixture.Create<PostgresException>(),
                _fixture.Create<Exception>()!);
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
        
        private Exception CreateException(Exception postgresException)
        {
            var exception = new Exception(_fixture.Create<string>(), postgresException);
            return exception;
        }

        private readonly PostgresDataExceptionManager _postgresDataExceptionManager;
        private readonly Fixture _fixture;
    }
}