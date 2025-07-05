using System;
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

        public static IEnumerable<T> Replace<T>(this IEnumerable<T> collection, Func<T, bool> matchFunc, Func<T, T> replaceFunc)
        {
            return collection.Select(arg => matchFunc(arg) ? replaceFunc(arg) : arg);
        }

        public static IEnumerable<T> Replace<T>(this IEnumerable<T> collection, Func<T, int, bool> matchFunc, Func<T, int, T> replaceFunc)
        {
            return collection.Select((arg, i) => matchFunc(arg, i) ? replaceFunc(arg, i) : arg);
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (var obj in source)
            {
                action(obj);
            }
        }

        /// <summary>
        /// Асинхронно выполняет указанное действие для каждого элемента в коллекции.
        /// Действие не поддерживает токен отмены.
        /// </summary>
        /// <typeparam name="T">Тип элемента.</typeparam>
        /// <param name="source">Исходная коллекция.</param>
        /// <param name="action">Асинхронное действие.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        /// <returns>Задача, представляющая выполнение всех действий.</returns>
        public static async Task ForEachAsync<T>(
            this IEnumerable<T> source, 
            Func<T, Task> action, 
            CancellationToken cancellationToken = default
            )
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            foreach (var obj in source)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await action(obj).ConfigureAwait(false);
            }
        }

        public static void NullSafeForEach<T>(this IEnumerable<T>? source, Action<T> action)
        {
            if (source != null) ForEach(source, action);
        }

        public static void For<T>(this IEnumerable<T> source, Action<int, T> action)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            T[] array = source.ToArray();
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

            T[] enumerable = source as T[] ?? source.ToArray();
            int skip = RndGen.Value.Next(enumerable.Length);
            return enumerable.Skip(skip).First();
        }

        public static List<T> Append<T>(this IEnumerable<T> source, params T[] elements)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (elements == null)
                throw new ArgumentNullException(nameof(elements));

            List<T> result = source.ToList();
            result.AddRange(elements);
            return result;
        }

        public static T[] Append<T>(this T[] source, params T[] elements)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return ((IEnumerable<T>) source).Append(elements).ToArray();
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
        {
            return source == null || !source.Any();
        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> instance, int partitionSize)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (partitionSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(partitionSize));

            return instance
                .Select((value, index) => new {Index = index, Value = value})
                .GroupBy(item => item.Index / partitionSize)
                .Select(item => item.Select(x => x.Value));
        }
    }
}