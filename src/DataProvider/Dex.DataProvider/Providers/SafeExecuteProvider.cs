using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Dex.DataProvider.Contracts;

namespace Dex.DataProvider.Providers
{
    public class SafeExecuteProvider : ISafeExecuteProvider
    {
        private readonly IDataExceptionManager _dataExceptionManager;

        public SafeExecuteProvider(IDataExceptionManager dataExceptionManager)
        {
            _dataExceptionManager = dataExceptionManager 
                                    ?? throw new ArgumentNullException(nameof(dataExceptionManager));
        }

        public Task<T> SafeExecuteAsync<T>(
            IDataProvider provider,
            Func<IDataProvider, CancellationToken, Task<T>> func,
            IsolationLevel level = IsolationLevel.RepeatableRead,
            int retryCount = 3,
            CancellationToken token = default)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (func == null) throw new ArgumentNullException(nameof(func));
            
            return SafeExecuteAsync(provider, WrapperTaskT, func, level, retryCount, token);
        }

        public Task SafeExecuteAsync(
            IDataProvider provider,
            Func<IDataProvider, CancellationToken, Task> func,
            IsolationLevel level = IsolationLevel.RepeatableRead,
            int retryCount = 3,
            CancellationToken token = default)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (func == null) throw new ArgumentNullException(nameof(func));
            
            return SafeExecuteAsync(provider, WrapperTask, func, level, retryCount, token);
        }

        private async Task<T> SafeExecuteAsync<T, TArg>(
            IDataProvider provider,
            Func<IDataProvider, TArg, CancellationToken, Task<T>> func,
            TArg arg,
            IsolationLevel level,
            int retryCount,
            CancellationToken token)
        {
            T result;
            var count = 0;
            while (true)
            {
                try
                {
                    using var transaction = provider.Transaction(level);
                    result = await func(provider, arg, token).ConfigureAwait(false);
                    transaction.Complete();
                    break;
                }
                catch (Exception exception)
                {
                    Reset(provider);

                    if (_dataExceptionManager.IsRepeatAction(exception) && ++count >= retryCount)
                    {
                        throw;
                    }
                }
                
                await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
            }

            return result;
        }

        private static void Reset(IDataProvider provider)
        {
            provider.Reset();
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct VoidStruct
        {
        }

        private static async Task<VoidStruct> WrapperTask(
            IDataProvider dp,
            Func<IDataProvider, CancellationToken, Task> f,
            CancellationToken t)
        {
            await f(dp, t);
            return default;
        }

        private static Task<T> WrapperTaskT<T>(
            IDataProvider dp,
            Func<IDataProvider, CancellationToken, Task<T>> f,
            CancellationToken t)
        {
            return f(dp, t);
        }
    }
}