using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Dex.Extensions
{
    public static class EnumerableExtension
    {
        private static readonly Lazy<Random> RndGen = new Lazy<Random>(() => new Random(DateTime.UtcNow.Millisecond));

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

        public static void ForEach<T>([NotNull] this IEnumerable<T> source, [NotNull] Action<T> action)
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

        public static async Task ForEachAsync<T>([NotNull] this IEnumerable<T> source, [NotNull] Func<T, Task> action)
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
                await action(obj).ConfigureAwait(false);
            }
        }

        public static void NullSafeForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null || action == null)
            {
                return;
            }

            ForEach(source, action);
        }

        public static void For<T>([NotNull] this IEnumerable<T> source, [NotNull] Action<int, T> action)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var array = source.ToArray();
            for (var i = 0; i < array.Length; i++)
            {
                var obj = array[i];
                action(i, obj);
            }
        }

        public static bool NullSafeAny<T>(this IEnumerable<T> source, Func<T, bool> predicate = null)
        {
            if (source == null) return false;

            return predicate == null
                ? source.Any()
                : source.Any(predicate);
        }

        public static T Random<T>([NotNull] this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var enumerable = source as T[] ?? source.ToArray();
            var skip = RndGen.Value.Next(enumerable.Length);
            return enumerable.Skip(skip).First();
        }

        public static IEnumerable<T> Append<T>([NotNull] this IEnumerable<T> source, [NotNull] params T[] elements)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var result = source.ToList();
            result.AddRange(elements);
            return result;
        }

        public static T[] Append<T>([NotNull] this T[] source, [NotNull] params T[] elements)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return ((IEnumerable<T>) source).Append(elements).ToArray();
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }

        public static IEnumerable<IEnumerable<T>> Split<T>([NotNull] this IEnumerable<T> instance, int partitionSize)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (partitionSize <= 0) throw new ArgumentOutOfRangeException(nameof(partitionSize));

            return instance
                .Select((value, index) => new {Index = index, Value = value})
                .GroupBy(item => item.Index / partitionSize)
                .Select(item => item.Select(x => x.Value));
        }
    }
}