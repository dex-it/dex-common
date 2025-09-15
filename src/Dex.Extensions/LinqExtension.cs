namespace System.Linq
{
    using System;
    using System.Collections.Generic;

    public static class LinqExtension
    {
        public static IEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(this IEnumerable<TOuter> outer,
            IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector, Func<TOuter, TInner?, TResult> resultSelector)
        {
            return outer.GroupJoin(inner, outerKeySelector, innerKeySelector, (outer, inners) => new { outer, inners })
                .SelectMany(group => group.inners.DefaultIfEmpty(), (x, inner) => resultSelector(x.outer, inner));
        }
    }
}
