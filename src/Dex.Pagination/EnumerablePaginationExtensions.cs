using System;
using System.Collections.Generic;
using System.Linq;
using Dex.Pagination.Conditions;

namespace Dex.Pagination;

public static class EnumerablePaginationExtensions
{
    /// <summary>
    /// Paging filter for IEnumerable
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">source query</param>
    /// <param name="page">page number</param>
    /// <param name="pageSize">page size</param>
    /// <returns></returns>
    public static IEnumerable<T> FilterPage<T>(this IEnumerable<T> source, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(source);

        // setup defaults
        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);

        return source.Skip((page - 1) * pageSize).Take(pageSize);
    }

    /// <summary>
    /// Paging filter for IEnumerable
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">source query</param>
    /// <param name="pageCondition">this interface with page number and page size</param>
    /// <param name="maxPageSize">limit PageSize parameter</param>
    /// <returns></returns>
    public static IEnumerable<T> FilterPage<T>(this IEnumerable<T> source, IPageCondition pageCondition, int? maxPageSize = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(pageCondition);
        if (maxPageSize is < 0)
            throw new ArgumentOutOfRangeException(nameof(maxPageSize), "can't be bellow zero");

        var pageSize = maxPageSize.HasValue ? Math.Min(maxPageSize.Value, pageCondition.PageSize) : pageCondition.PageSize;
        return source.FilterPage(pageCondition.Page, pageSize);
    }
}