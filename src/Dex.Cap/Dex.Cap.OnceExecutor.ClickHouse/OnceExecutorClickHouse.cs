﻿using System;
using System.Threading;
using System.Threading.Tasks;
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

        protected override Task<TResult?> ExecuteInTransaction<TResult>(Guid idempotentKey, Func<CancellationToken, Task<TResult?>> operation,
            CancellationToken cancellation)
            where TResult : default
        {
            return operation(cancellation);
        }

        protected override Task OnModificationComplete()
        {
            return Task.CompletedTask;
        }

        protected override async Task<bool> IsAlreadyExecuted(Guid idempotentKey, CancellationToken cancellationToken)
        {
            await Context.OpenAsync(cancellationToken);

            await using (var command = Context.CreateCommand(_createTableCommandText))
            {
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var command = Context.CreateCommand(
                             $"SELECT count() FROM {LastTransaction.TableName} WHERE {nameof(LastTransaction.IdempotentKey)}=@key"))
            {
                command.Parameters.AddWithValue("key", idempotentKey.ToString("N"));

                var count = await command.ExecuteScalarAsync<ulong>(cancellationToken);
                return count > 0;
            }
        }

        protected override async Task SaveIdempotentKey(Guid idempotentKey, CancellationToken cancellationToken)
        {
            await using (var command =
                         Context.CreateCommand($"INSERT INTO {LastTransaction.TableName} SELECT @key, @cd"))
            {
                command.Parameters.AddWithValue("key", idempotentKey.ToString("N"));
                command.Parameters.AddWithValue("cd", DateTime.UtcNow.ToString("s"));

                await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }
}