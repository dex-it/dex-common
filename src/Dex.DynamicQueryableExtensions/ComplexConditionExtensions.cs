using System.Linq;
using Dex.DynamicQueryableExtensions.Data;

namespace Dex.DynamicQueryableExtensions
{
    public static class ComplexConditionExtensions
    {
        public static IQueryable<T> ApplyCondition<T>(this IQueryable<T> source, IComplexQueryCondition condition)
        {
            if (condition == null)
                return source;

            if (condition.FilterCondition != null)
                source = source.Filter(condition.FilterCondition);

            if (condition.SortCondition != null)
                source = source.SortByParams(condition.SortCondition);

            if (condition.Page > 0 && condition.PageSize > 0)
                source = source.FilterPage(condition.Page, condition.PageSize);

            return source;
        }
    }
}