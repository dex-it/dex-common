using System;
using System.Collections.Generic;
using System.Linq;

namespace Dex.Extensions
{
    public static class LinqExtension
    {
#if NET5_0_OR_GREATER
        public static IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this IEnumerable<TOuter> outer,
            IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner?, TResult> resultSelector)
        {
            return outer.GroupJoin(inner, outerKeySelector, innerKeySelector,
                    (tOuter, inners) => new {outer = tOuter, inners})
                .SelectMany(group => group.inners.DefaultIfEmpty(), (x, tInner) => resultSelector(x.outer, tInner));
        }
#else
        public static IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this IEnumerable<TOuter> outer,
            IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner?, TResult> resultSelector) where TInner : class
        {
            return outer.GroupJoin(inner, outerKeySelector, innerKeySelector,
                    (tOuter, inners) => new {outer = tOuter, inners})
                .SelectMany(group => group.inners.DefaultIfEmpty(), (x, tInner) => resultSelector(x.outer, tInner));
        }

        public static IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this IEnumerable<TOuter> outer,
            IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner?, TResult> resultSelector) where TInner : struct
        {
            return outer.GroupJoin(inner, outerKeySelector, innerKeySelector,
                    (tOuter, inners) => new {outer = tOuter, inners})
                .SelectMany(group => group.inners.DefaultIfEmpty(), (x, tInner) => resultSelector(x.outer, tInner));
        }
#endif
    }
}