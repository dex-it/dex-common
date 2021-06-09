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
                    provider.Reset();

                    if (_dataExceptionManager.IsRepeatableException(exception) && ++count >= retryCount)
                    {
                        throw;
                    }
                }
                
                await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
            }

            return result;
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct VoidStruct
        {
        }
        
        private static readonly Task<VoidStruct> VoidStructTask = Task.FromResult<VoidStruct>(default);

        private static Task<VoidStruct> WrapperTask(
            IDataProvider dp,
            Func<IDataProvider, CancellationToken, Task> f,
            CancellationToken t)
        {
            var task = f(dp, t);
            
            if (task.IsCompletedSuccessfully)
            {
                return VoidStructTask;
            }
            else
            {
                return WaitAsync(task);
            }
            
            static async Task<VoidStruct> WaitAsync(Task task)
            {
                await task.ConfigureAwait(false);
                return default;
            }
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