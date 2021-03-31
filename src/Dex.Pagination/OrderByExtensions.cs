using System;
using System.Linq;
using System.Linq.Expressions;
using Dex.Pagination.Data;

// ReSharper disable MemberCanBePrivate.Global

namespace Dex.Pagination
{
    public static class OrderByExtensions
    {
        public static IQueryable<T> OrderByParams<T>(this IQueryable<T> source, ISortParam sortParam)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (sortParam == null)
                throw new ArgumentNullException(nameof(sortParam));

            return source.OrderByParams(new[] {sortParam});
        }

        public static IQueryable<T> OrderByParams<T>(this IQueryable<T> source, ISortParam[] sortParams)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (sortParams == null || !sortParams.Any())
                throw new ArgumentNullException(nameof(sortParams));

            var selector = BuildSelector<T>(sortParams[0].FieldName);
            var result = sortParams[0].IsDesc ? source.OrderByDescending(selector) : source.OrderBy(selector);

            for (var i = 1; i < sortParams.Length; i++)
            {
                selector = BuildSelector<T>(sortParams[i].FieldName);
                result = sortParams[i].IsDesc ? result.ThenByDescending(selector) : result.ThenBy(selector);
            }

            return result;
        }

        private static Expression<Func<TSource, IComparable>> BuildSelector<TSource>(string fieldName)
        {
            var param = Expression.Parameter(typeof(TSource));
            var member = fieldName.Split('.').Aggregate<string, Expression>(param, Expression.Property);

            return Expression.Lambda<Func<TSource, IComparable>>(Expression.Convert(member, typeof(IComparable)), param);
        }
    }
}