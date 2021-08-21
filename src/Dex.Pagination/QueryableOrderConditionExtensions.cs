using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Dex.Pagination.Conditions;

// ReSharper disable MemberCanBePrivate.Global

namespace Dex.Pagination
{
    public static class QueryableOrderConditionExtensions
    {
        /// <summary>
        /// Sorting for IQueryable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">source query</param>
        /// <param name="orderCondition">interface with properties FieldName and IsDescending</param>
        /// <returns></returns>
        public static IQueryable<T> OrderByParams<T>(this IQueryable<T> source, IOrderCondition orderCondition)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (orderCondition == null)
                throw new ArgumentNullException(nameof(orderCondition));

            return source.OrderByParams(new[] {orderCondition});
        }

        /// <summary>
        /// Sorting for IQueryable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">source query</param>
        /// <param name="conditions">array interface with properties FieldName and IsDescending</param>
        /// <returns></returns>
        public static IQueryable<T> OrderByParams<T>(this IQueryable<T> source, IEnumerable<IOrderCondition> conditions)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (conditions == null)
                throw new ArgumentNullException(nameof(conditions));

            var orderConditions = conditions.ToArray();
            if (!orderConditions.Any()) return source;

            var selector = BuildSelector<T>(orderConditions[0].FieldName);
            var result = orderConditions[0].IsDesc ? source.OrderByDescending(selector) : source.OrderBy(selector);

            for (var i = 1; i < orderConditions.Length; i++)
            {
                selector = BuildSelector<T>(orderConditions[i].FieldName);
                result = orderConditions[i].IsDesc ? result.ThenByDescending(selector) : result.ThenBy(selector);
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