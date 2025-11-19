using System;
using System.Collections.Generic;
using System.Linq;

namespace Dex.Extensions
{
    public static class LinqExtension
    {
        public static IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this IEnumerable<TOuter> outer,
            IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner?, TResult> resultSelector)
        {
            return outer.GroupJoin(inner, outerKeySelector, innerKeySelector,
                    (tOuter, inners) => new { outer = tOuter, inners })
                .SelectMany(group => group.inners.DefaultIfEmpty(), (x, tInner) => resultSelector(x.outer, tInner));
        }
    }
}