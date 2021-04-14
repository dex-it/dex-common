using System;
using System.Linq;
using Dex.DynamicQueryableExtensions.Data;

// ReSharper disable MemberCanBePrivate.Global

namespace Dex.DynamicQueryableExtensions
{
    public static class PaginationExtensions
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
        /// <param name="pageFilter">this interface with page number and page size</param>
        /// <returns></returns>
        public static IQueryable<T> FilterPage<T>(this IQueryable<T> source, IPageFilter pageFilter)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (pageFilter == null)
                throw new ArgumentNullException(nameof(pageFilter));

            return source.FilterPage(pageFilter.Page, pageFilter.PageSize);
        }
    }
}