using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Dex.Pagination.Conditions;

namespace Dex.Pagination;

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
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(orderCondition);

        return source.OrderByParams([orderCondition]);
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
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(conditions);

        var orderConditions = conditions.ToArray();
        if (orderConditions.Length == 0) return source;

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