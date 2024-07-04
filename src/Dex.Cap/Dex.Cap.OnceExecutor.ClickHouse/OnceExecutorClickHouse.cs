using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Dex.Cap.OnceExecutor.Models;
using Octonica.ClickHouseClient;

namespace Dex.Cap.OnceExecutor.ClickHouse
{
    [SuppressMessage("Reliability", "CA2007:Попробуйте вызвать ConfigureAwait для ожидаемой задачи")]
    public class OnceExecutorClickHouse : BaseOnceExecutor<IClickHouseOptions, ClickHouseConnection>
    {
        protected override ClickHouseConnection Context { get; }

        private readonly string _createTableCommandText =
            $"CREATE TABLE IF NOT EXISTS {LastTransaction.TableName} ({nameof(LastTransaction.IdempotentKey)} String,{nameof(LastTransaction.Created)} DateTime) ENGINE = TinyLog";

        public OnceExecutorClickHouse(ClickHouseConnection connection)
        {
            Context = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        protected override async Task<TResult?> ExecuteInTransactionAsync<TResult>(
            Func<CancellationToken, Task<TResult?>> operation,
            Func<CancellationToken, Task<bool>> verifySucceeded,
            IClickHouseOptions? options,
            CancellationToken cancellationToken)
            where TResult : default
        {
            ArgumentNullException.ThrowIfNull(operation);

            return await operation(cancellationToken).ConfigureAwait(false);
        }

        protected override Task OnModificationCompletedAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override async Task<bool> IsAlreadyExecutedAsync(string idempotentKey, CancellationToken cancellationToken)
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

        protected override async Task SaveIdempotentKeyAsync(string idempotentKey, CancellationToken cancellationToken)
        {
            await using var command = Context.CreateCommand($"INSERT INTO {LastTransaction.TableName} SELECT @key, @cd");
            command.Parameters.AddWithValue("key", idempotentKey);
            command.Parameters.AddWithValue("cd", DateTime.UtcNow.ToString("s"));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}