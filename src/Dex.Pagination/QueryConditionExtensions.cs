using System.Linq;
using Dex.Pagination.Conditions;
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable UnusedType.Global

namespace Dex.Pagination
{
    public static class QueryConditionExtensions
    {
        public static IQueryable<T> ApplyCondition<T>(this IQueryable<T> source, IQueryCondition condition)
        {
            if (condition == null)
                return source;

            if (condition.SortCondition is not null)
                source = source.OrderByParams(condition.SortCondition);

            if (condition.PageCondition is not null)
                source = source.FilterPage(condition.PageCondition);

            return source;
        }
    }
}