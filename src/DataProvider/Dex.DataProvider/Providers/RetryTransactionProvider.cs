using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.DataProvider.Contracts;
using Dex.DataProvider.Settings;

namespace Dex.DataProvider.Providers
{
    public sealed class RetryTransactionProvider : IRetryTransactionProvider
    {
        private readonly IDataExceptionManager _dataExceptionManager;
        private readonly IRetryTransactionSettings _settings;

        public RetryTransactionProvider(IDataExceptionManager dataExceptionManager, IRetryTransactionSettings settings)
        {
            _dataExceptionManager = dataExceptionManager
                                    ?? throw new ArgumentNullException(nameof(dataExceptionManager));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public Task<T> Execute<T, TArg>(
            IDataTransactionProvider provider,
            Func<TArg, CancellationToken, Task<T>> func,
            TArg arg,
            IsolationLevel level = IsolationLevel.RepeatableRead,
            int retryCount = 3,
            CancellationToken cancellationToken = default)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            if (retryCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryCount));
            }


            return InternalExecute(provider, func, arg, level, retryCount, cancellationToken);
        }

        public Task<T> Execute<T>(
            IDataTransactionProvider provider,
            Func<CancellationToken, Task<T>> func,
            IsolationLevel level = IsolationLevel.RepeatableRead,
            int retryCount = 3,
            CancellationToken cancellationToken = default)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }
            
            if (retryCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryCount));
            }

            return InternalExecute(provider, WrapperTaskT, func, level, retryCount, cancellationToken);
        }

        public Task Execute<TArg>(
            IDataTransactionProvider provider,
            Func<TArg, CancellationToken, Task> func,
            TArg arg,
            IsolationLevel level = IsolationLevel.RepeatableRead,
            int retryCount = 3,
            CancellationToken cancellationToken = default)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }
            
            if (retryCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryCount));
            }

            return InternalExecute(provider, WrapperTaskArg, (func, arg), level, retryCount, cancellationToken);
        }

        public Task Execute(
            IDataTransactionProvider provider,
            Func<CancellationToken, Task> func,
            IsolationLevel level = IsolationLevel.RepeatableRead,
            int retryCount = 3,
            CancellationToken cancellationToken = default)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }
            
            if (retryCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryCount));
            }

            return InternalExecute(provider, WrapperTask, func, level, retryCount, cancellationToken);
        }

        private async Task<T> InternalExecute<T, TArg>(
            IDataTransactionProvider provider,
            Func<TArg, CancellationToken, Task<T>> func,
            TArg arg,
            IsolationLevel level,
            int retryCount,
            CancellationToken cancellationToken)
        {
            var count = 0;
            Repeat:

            try
            {
                using var transaction = provider.Transaction(level);
                var result = await func(arg, cancellationToken).ConfigureAwait(false);
                transaction.Complete();
                return result;
            }
            catch (Exception exception)
                when (_dataExceptionManager.IsRepeatableException(exception) && ++count < retryCount)
            {
                await Task.Delay(_settings.RetryDelay, cancellationToken).ConfigureAwait(false);
                goto Repeat;
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct VoidStruct
        {
        }

        private static async Task<VoidStruct> WrapperTask(
            Func<CancellationToken, Task> func,
            CancellationToken cancellationToken)
        {
            await func(cancellationToken).ConfigureAwait(false);
            return default;
        }

        private static async Task<VoidStruct> WrapperTaskArg<TArg>(
            (Func<TArg, CancellationToken, Task> func, TArg arg) tuple,
            CancellationToken cancellationToken)
        {
            await tuple.func(tuple.arg, cancellationToken).ConfigureAwait(false);
            return default;
        }

        private static Task<T> WrapperTaskT<T>(
            Func<CancellationToken, Task<T>> func,
            CancellationToken cancellationToken)
        {
            return func(cancellationToken);
        }
    }
}