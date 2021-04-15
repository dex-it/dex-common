using System.Linq;
using Dex.DynamicQueryableExtensions.Conditions;
// ReSharper disable UnusedType.Global

namespace Dex.DynamicQueryableExtensions
{
    public static class QueryConditionExtensions
    {
        public static IQueryable<T> ApplyCondition<T>(this IQueryable<T> source, IQueryCondition condition)
        {
            if (condition == null)
                return source;

            if (condition.FilterCondition != null)
                source = source.Filter(condition.FilterCondition);

            if (condition.SortCondition != null)
                source = source.OrderByParams(condition.SortCondition);

            if (condition.Page > 0 && condition.PageSize > 0)
                source = source.FilterPage(condition.Page, condition.PageSize);

            return source;
        }
    }
}