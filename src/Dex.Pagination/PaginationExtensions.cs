using System;
using System.Linq;
using Dex.Pagination.Conditions;

// ReSharper disable MemberCanBePrivate.Global

namespace Dex.Pagination
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
        /// <param name="pageCondition">this interface with page number and page size</param>
        /// <returns></returns>
        public static IQueryable<T> FilterPage<T>(this IQueryable<T> source, IPageCondition pageCondition)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (pageCondition == null)
                throw new ArgumentNullException(nameof(pageCondition));

            return source.FilterPage(pageCondition.Page, pageCondition.PageSize);
        }
    }
}