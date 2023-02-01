using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.Cap.OnceExecutor.Models;
using Octonica.ClickHouseClient;

namespace Dex.Cap.OnceExecutor.ClickHouse
{
    public class OnceExecutorClickHouse : BaseOnceExecutor<ClickHouseConnection>
    {
        protected override ClickHouseConnection Context { get; }

        private readonly string _createTableCommandText =
            $"CREATE TABLE IF NOT EXISTS {LastTransaction.TableName} ({nameof(LastTransaction.IdempotentKey)} String,{nameof(LastTransaction.Created)} DateTime) ENGINE = TinyLog";

        public OnceExecutorClickHouse(ClickHouseConnection connection)
        {
            Context = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        protected override Task<TResult?> ExecuteInTransaction<TResult>(
            Func<CancellationToken, Task<TResult?>> operation,
            TransactionScopeOption transactionScopeOption,
            IsolationLevel isolationLevel,
            CancellationToken cancellation)
            where TResult : default
        {
            return operation(cancellation);
        }

        protected override Task OnModificationCompleted(CancellationToken cancellation)
        {
            return Task.CompletedTask;
        }

        protected override async Task<bool> IsAlreadyExecuted(string idempotentKey, CancellationToken cancellationToken)
        {
            await Context.OpenAsync(cancellationToken);

            await using (var command = Context.CreateCommand(_createTableCommandText))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var command = Context.CreateCommand(
                             $"SELECT count() FROM {LastTransaction.TableName} WHERE {nameof(LastTransaction.IdempotentKey)}=@key"))
            {
                command.Parameters.AddWithValue("key", idempotentKey);

                var count = await command.ExecuteScalarAsync<ulong>(cancellationToken);
                return count > 0;
            }
        }

        protected override async Task SaveIdempotentKey(string idempotentKey, CancellationToken cancellationToken)
        {
            await using (var command =
                         Context.CreateCommand($"INSERT INTO {LastTransaction.TableName} SELECT @key, @cd"))
            {
                command.Parameters.AddWithValue("key", idempotentKey);
                command.Parameters.AddWithValue("cd", DateTime.UtcNow.ToString("s"));

                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }
}