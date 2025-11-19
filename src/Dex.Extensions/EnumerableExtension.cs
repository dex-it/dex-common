using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Extensions
{
    public static class EnumerableExtension
    {
        private static readonly Lazy<Random> RndGen = new(() => new Random(DateTime.UtcNow.Millisecond));

        public static string JoinToString(this IEnumerable<string> collection, string separator)
        {
            return string.Join(separator, collection);
        }

        public static IEnumerable<T> Replace<T>(this IEnumerable<T> collection, Func<T, bool> matchFunc,
            Func<T, T> replaceFunc)
        {
            return collection.Select(arg => matchFunc(arg) ? replaceFunc(arg) : arg);
        }

        public static IEnumerable<T> Replace<T>(this IEnumerable<T> collection, Func<T, int, bool> matchFunc,
            Func<T, int, T> replaceFunc)
        {
            return collection.Select((arg, i) => matchFunc(arg, i) ? replaceFunc(arg, i) : arg);
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            foreach (var obj in source)
            {
                action(obj);
            }
        }

        #region ForEachAsync (IEnumerable)

        /// <summary>
        /// Асинхронно выполняет указанное действие для каждого элемента в коллекции.
        /// Действие не поддерживает токен отмены.
        /// </summary>
        /// <typeparam name="T">Тип элемента.</typeparam>
        /// <param name="source">Исходная коллекция.</param>
        /// <param name="action">Асинхронное действие.</param>
        /// <param name="continueOnError">Если <c>true</c>, исключения в действии будут проигнорированы, но в конце
        /// выбросится AggregateException.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача, представляющая выполнение всех действий.</returns>
        public static async Task ForEachAsync<T>(
            this IEnumerable<T> source,
            Func<T, Task> action,
            bool continueOnError = false,
            CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            List<Exception> exceptions = [];

            foreach (var obj in source)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await action(obj).ConfigureAwait(false);
                }
                catch (Exception exception) when (continueOnError)
                {
                    exceptions.Add(exception);
                }
            }

            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }

        /// <summary>
        /// Асинхронно выполняет указанное действие для каждого элемента в коллекции.
        /// Действие не поддерживает токен отмены.
        /// </summary>
        /// <typeparam name="T">Тип элемента.</typeparam>
        /// <param name="source">Исходная коллекция.</param>
        /// <param name="action">Асинхронное действие.</param>
        /// <param name="continueOnError">Если <c>true</c>, исключения в действии будут проигнорированы, но в конце
        /// выбросится AggregateException.</param>
        /// <param name="maxDegreeOfParallelism">Максимальное количество задач, обрабатываемых одновременно.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача, представляющая выполнение всех действий.</returns>
        public static async Task ForEachAsync<T>(
            this IEnumerable<T> source,
            Func<T, Task> action,
            int maxDegreeOfParallelism,
            bool continueOnError = false,
            CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (maxDegreeOfParallelism < 1)
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));

            ConcurrentBag<Exception> exceptions = [];
            List<Task> tasks = [];

            using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

            foreach (var obj in source)
            {
                var localObj = obj;
                var localSemaphore = semaphore;

                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    break;

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await action(localObj).ConfigureAwait(false);
                    }
                    catch (Exception exception) when (continueOnError)
                    {
                        exceptions.Add(exception);
                    }
                    finally
                    {
                        localSemaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            if (!exceptions.IsEmpty)
                throw new AggregateException(exceptions);
        }

        /// <summary>
        /// Асинхронно выполняет указанное действие для каждого элемента в коллекции.
        /// Действие поддерживает токен отмены.
        /// </summary>
        /// <typeparam name="T">Тип элемента.</typeparam>
        /// <param name="source">Исходная коллекция.</param>
        /// <param name="action">Асинхронное действие с поддержкой CancellationToken.</param>
        /// <param name="continueOnError">Если <c>true</c>, исключения в действии будут проигнорированы, но в конце
        /// выбросится AggregateException.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача, представляющая выполнение всех действий.</returns>
        public static async Task ForEachAsync<T>(
            this IEnumerable<T> source,
            Func<T, CancellationToken, Task> action,
            bool continueOnError = false,
            CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            List<Exception> exceptions = [];

            foreach (var obj in source)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await action(obj, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (continueOnError)
                {
                    exceptions.Add(exception);
                }
            }

            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }

        /// <summary>
        /// Асинхронно выполняет указанное действие для каждого элемента в коллекции.
        /// Действие поддерживает токен отмены.
        /// </summary>
        /// <typeparam name="T">Тип элемента.</typeparam>
        /// <param name="source">Исходная коллекция.</param>
        /// <param name="action">Асинхронное действие с поддержкой CancellationToken.</param>
        /// <param name="maxDegreeOfParallelism">Максимальное количество задач, обрабатываемых одновременно.</param>
        /// <param name="continueOnError">Если <c>true</c>, исключения в действии будут проигнорированы, но в конце
        /// выбросится AggregateException.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача, представляющая выполнение всех действий.</returns>
        public static async Task ForEachAsync<T>(
            this IEnumerable<T> source,
            Func<T, CancellationToken, Task> action,
            int maxDegreeOfParallelism,
            bool continueOnError = false,
            CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (maxDegreeOfParallelism < 1)
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));

            List<Exception> exceptions = [];
            List<Task> tasks = [];

            using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

            foreach (var obj in source)
            {
                var localObj = obj;
                var localSemaphore = semaphore;

                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await action(localObj, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception exception) when (continueOnError)
                    {
                        exceptions.Add(exception);
                    }
                    finally
                    {
                        localSemaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }

        #endregion

        #region ForEachAsync (IAsyncEnumerable)

        /// <summary>
        /// Асинхронно выполняет указанное действие для каждого элемента в асинхронной коллекции.
        /// Действие не поддерживает токен отмены.
        /// </summary>
        /// <typeparam name="T">Тип элемента.</typeparam>
        /// <param name="source">Исходная коллекция.</param>
        /// <param name="action">Асинхронное действие.</param>
        /// <param name="continueOnError">Если <c>true</c>, исключения в действии будут проигнорированы, но в конце
        /// выбросится AggregateException.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача, представляющая выполнение всех действий.</returns>
        public static async Task ForEachAsync<T>(
            this IAsyncEnumerable<T> source,
            Func<T, Task> action,
            bool continueOnError = false,
            CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            List<Exception> exceptions = [];

            await foreach (var obj in source.ConfigureAwait(false).WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await action(obj).ConfigureAwait(false);
                }
                catch (Exception exception) when (continueOnError)
                {
                    exceptions.Add(exception);
                }
            }

            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }

        /// <summary>
        /// Асинхронно выполняет указанное действие для каждого элемента в асинхронной коллекции.
        /// Действие не поддерживает токен отмены.
        /// </summary>
        /// <typeparam name="T">Тип элемента.</typeparam>
        /// <param name="source">Исходная коллекция.</param>
        /// <param name="action">Асинхронное действие.</param>
        /// <param name="maxDegreeOfParallelism">Максимальное количество задач, обрабатываемых одновременно.</param>
        /// <param name="continueOnError">Если <c>true</c>, исключения в действии будут проигнорированы, но в конце
        /// выбросится AggregateException.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача, представляющая выполнение всех действий.</returns>
        public static async Task ForEachAsync<T>(
            this IAsyncEnumerable<T> source,
            Func<T, Task> action,
            int maxDegreeOfParallelism,
            bool continueOnError = false,
            CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (maxDegreeOfParallelism < 1)
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));

            ConcurrentBag<Exception> exceptions = [];
            List<Task> tasks = [];

            using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

            await foreach (var obj in source.ConfigureAwait(false).WithCancellation(cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    break;

                var localObj = obj;
                var localSemaphore = semaphore;

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await action(localObj).ConfigureAwait(false);
                    }
                    catch (Exception exception) when (continueOnError)
                    {
                        exceptions.Add(exception);
                    }
                    finally
                    {
                        localSemaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            if (!exceptions.IsEmpty)
                throw new AggregateException(exceptions);
        }

        /// <summary>
        /// Асинхронно выполняет указанное действие для каждого элемента в асинхронной коллекции.
        /// Действие поддерживает токен отмены.
        /// </summary>
        /// <typeparam name="T">Тип элемента.</typeparam>
        /// <param name="source">Исходная коллекция.</param>
        /// <param name="action">Асинхронное действие с поддержкой CancellationToken.</param>
        /// <param name="continueOnError">Если <c>true</c>, исключения в действии будут проигнорированы, но в конце
        /// выбросится AggregateException.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача, представляющая выполнение всех действий.</returns>
        public static async Task ForEachAsync<T>(
            this IAsyncEnumerable<T> source,
            Func<T, CancellationToken, Task> action,
            bool continueOnError = false,
            CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            List<Exception> exceptions = [];

            await foreach (var obj in source.ConfigureAwait(false).WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await action(obj, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (continueOnError)
                {
                    exceptions.Add(exception);
                }
            }

            if (exceptions.Any())
                throw new AggregateException(exceptions);
        }

        /// <summary>
        /// Асинхронно выполняет указанное действие для каждого элемента в асинхронной коллекции.
        /// Действие поддерживает токен отмены.
        /// </summary>
        /// <typeparam name="T">Тип элемента.</typeparam>
        /// <param name="source">Исходная коллекция.</param>
        /// <param name="action">Асинхронное действие с поддержкой CancellationToken.</param>
        /// <param name="maxDegreeOfParallelism">Максимальное количество задач, обрабатываемых одновременно.</param>
        /// <param name="continueOnError">Если <c>true</c>, исключения в действии будут проигнорированы, но в конце
        /// выбросится AggregateException.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача, представляющая выполнение всех действий.</returns>
        public static async Task ForEachAsync<T>(
            this IAsyncEnumerable<T> source,
            Func<T, CancellationToken, Task> action,
            int maxDegreeOfParallelism,
            bool continueOnError = false,
            CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (maxDegreeOfParallelism < 1)
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));

            ConcurrentBag<Exception> exceptions = [];
            List<Task> tasks = [];

            using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);

            await foreach (var obj in source.ConfigureAwait(false).WithCancellation(cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                    break;

                var localObj = obj;
                var localSemaphore = semaphore;

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await action(localObj, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception exception) when (continueOnError)
                    {
                        exceptions.Add(exception);
                    }
                    finally
                    {
                        localSemaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);

            if (!exceptions.IsEmpty)
                throw new AggregateException(exceptions);
        }

        #endregion

        public static void NullSafeForEach<T>(this IEnumerable<T>? source, Action<T> action)
        {
            if (source != null) ForEach(source, action);
        }

        public static void For<T>(this IEnumerable<T> source, Action<int, T> action)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var array = source.ToArray();
            for (var i = 0; i < array.Length; i++)
            {
                T obj = array[i];
                action(i, obj);
            }
        }

        public static bool NullSafeAny<T>(this IEnumerable<T>? source, Func<T, bool>? predicate = null)
        {
            return predicate == null
                ? source != null && source.Any()
                : source != null && source.Any(predicate);
        }

        public static T Random<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var enumerable = source as T[] ?? source.ToArray();
            var skip = RndGen.Value.Next(enumerable.Length);
            return enumerable.Skip(skip).First();
        }

        public static List<T> Append<T>(this IEnumerable<T> source, params T[] elements)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            var result = source.ToList();
            result.AddRange(elements);
            return result;
        }

        public static T[] Append<T>(this T[] source, params T[] elements)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return ((IEnumerable<T>)source).Append(elements).ToArray();
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
        {
            return source == null || !source.Any();
        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> instance, int partitionSize)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (partitionSize <= 0) throw new ArgumentOutOfRangeException(nameof(partitionSize));

            return instance
                .Select((value, index) => new { Index = index, Value = value })
                .GroupBy(item => item.Index / partitionSize)
                .Select(item => item.Select(x => x.Value));
        }
    }
}