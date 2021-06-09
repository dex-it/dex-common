using System;
using Dex.DataProvider.Contracts;
using Dex.DataProvider.Exceptions;
using Npgsql;

namespace Dex.DataProvider.Postgres
{
    public class PostgresDataExceptionManager : IDataExceptionManager
    {
        public DataProviderException Normalize(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            if (exception.InnerException is PostgresException postgresException)
            {
                var dataProviderException = NormalizePostgresException(postgresException, exception);
                if (dataProviderException is not null)
                {
                    return dataProviderException;
                }
            }

            return new DataProviderException(exception.Message, exception);
        }

        public virtual bool IsRepeatableException(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            return exception is ConcurrentModifyException
                   || exception.InnerException is PostgresException {SqlState: PostgresErrorCodes.SerializationFailure};
        }

        protected virtual DataProviderException? NormalizePostgresException(
            PostgresException postgresException,
            Exception innerException)
        {
            var message = postgresException.Message + postgresException.Detail;

            return postgresException.SqlState switch
            {
                PostgresErrorCodes.ForeignKeyViolation => new ForeignKeyViolationException(message, innerException),
                PostgresErrorCodes.UniqueViolation => new ObjectAlreadyExistsException(message, innerException),
                PostgresErrorCodes.SerializationFailure => new ConcurrentModifyException(message, innerException),
                _ => null
            };
        }
    }
}