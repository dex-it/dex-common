using System;
using Dex.DataProvider.Postgres;
using Npgsql;

namespace Dex.DataProvider.Tests.Helpers
{
    internal class PostgresDataExceptionManagerTest : PostgresDataExceptionManager
    {
        public void NormalizePostgresExceptionTest(
            PostgresException postgresException,
            Exception innerException)
        {
            NormalizePostgresException(postgresException, innerException);
        }
    }
}