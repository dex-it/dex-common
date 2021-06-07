using System;
using Dex.DataProvider.Contracts;
using Dex.DataProvider.Exceptions;
using Npgsql;

namespace Dex.DataProvider.Postgres
{
    public sealed class PostgresDataExceptionManager : IDataExceptionManager
    {
        public Exception Normalize(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            if (exception.InnerException is PostgresException postgresException)
            {
                var message = postgresException.Message + postgresException.Detail;

                switch (postgresException.SqlState)
                {
                    case PostgresErrorCodes.ForeignKeyViolation:
                        return new ForeignKeyViolationException(message, exception);
                    case PostgresErrorCodes.UniqueViolation:
                        return new ObjectAlreadyExistsException(message, exception);
                    case PostgresErrorCodes.SerializationFailure:
                        return new ConcurrentModifyException(message, exception);
                }
            }

            return new DataProviderException(exception.Message, exception);
        }

        public bool IsRepeatAction(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            return exception is ConcurrentModifyException
                   || exception.InnerException is PostgresException {SqlState: PostgresErrorCodes.SerializationFailure};
        }
    }
}