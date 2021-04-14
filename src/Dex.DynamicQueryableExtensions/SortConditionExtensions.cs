using System;
using System.Linq;
using System.Linq.Expressions;
using Dex.DynamicQueryableExtensions.Data;

// ReSharper disable MemberCanBePrivate.Global

namespace Dex.DynamicQueryableExtensions
{
    public static class SortConditionExtensions
    {
        /// <summary>
        /// Sorting for IQueryable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">source query</param>
        /// <param name="sortCondition">interface with properties FieldName and IsDescending</param>
        /// <returns></returns>
        public static IQueryable<T> SortByParams<T>(this IQueryable<T> source, ISortCondition sortCondition)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (sortCondition == null)
                throw new ArgumentNullException(nameof(sortCondition));

            return source.SortByParams(new[] {sortCondition});
        }

        /// <summary>
        /// Sorting for IQueryable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">source query</param>
        /// <param name="sortParams">array interface with properties FieldName and IsDescending</param>
        /// <returns></returns>
        public static IQueryable<T> SortByParams<T>(this IQueryable<T> source, ISortCondition[] sortParams)
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

        private static Expression<Func<TSource, object>> BuildSelector<TSource>(string fieldName)
        {
            var param = Expression.Parameter(typeof(TSource));
            var member = fieldName.Split('.').Aggregate<string, Expression>(param, Expression.Property);

            if (!typeof(IComparable).IsAssignableFrom(member.Type))
                throw new InvalidCastException("Member type is not IComparable");

            return Expression.Lambda<Func<TSource, object>>(Expression.Convert(member, typeof(object)), param);
        }
    }
}