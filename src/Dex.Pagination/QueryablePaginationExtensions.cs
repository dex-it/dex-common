using System;
using System.Linq;
using Dex.Pagination.Conditions;

// ReSharper disable MemberCanBePrivate.Global

namespace Dex.Pagination
{
    public static class QueryablePaginationExtensions
    {
        /// <summary>
        /// Paging filter for IQueryable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">source query</param>
        /// <param name="page">page number</param>
        /// <param name="pageSize">page size</param>
        /// <returns></returns>
        public static IQueryable<T> FilterPage<T>(this IQueryable<T> source, int page, int pageSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // setup defaults
            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);

            return source.Skip((page - 1) * pageSize).Take(pageSize);
        }

        /// <summary>
        /// Paging filter for IQueryable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">source query</param>
        /// <param name="pageCondition">this interface with page number and page size</param>
        /// <param name="maxPageSize">limit PageSize parameter</param>
        /// <returns></returns>
        public static IQueryable<T> FilterPage<T>(this IQueryable<T> source, IPageCondition pageCondition, int? maxPageSize = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (pageCondition == null)
                throw new ArgumentNullException(nameof(pageCondition));
            if (maxPageSize is < 0)
                throw new ArgumentOutOfRangeException(nameof(maxPageSize), "can't be bellow zero");

            var pageSize = maxPageSize.HasValue ? Math.Min(maxPageSize.Value, pageCondition.PageSize) : pageCondition.PageSize;
            return source.FilterPage(pageCondition.Page, pageSize);
        }
    }
}