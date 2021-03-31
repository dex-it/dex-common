using System;
using System.Linq;
using Dex.Pagination.Data;

// ReSharper disable MemberCanBePrivate.Global

namespace Dex.Pagination
{
    public static class PaginationQueryExtensions
    {
        public static IQueryable<T> FilterPage<T>(this IQueryable<T> source, int page, int pageSize)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            // setup defaults
            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);

            return source.Skip((page - 1) * pageSize).Take(pageSize);
        }

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