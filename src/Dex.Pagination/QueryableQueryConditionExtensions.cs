using System.Linq;
using Dex.Pagination.Conditions;

namespace Dex.Pagination;

public static class QueryableQueryConditionExtensions
{
    public static IQueryable<T> ApplyCondition<T>(this IQueryable<T> source, IQueryCondition? condition)
    {
        if (condition == null)
            return source;

        if (condition.OrderConditions is not null)
            source = source.OrderByParams(condition.OrderConditions);

        if (condition.PageCondition is not null)
            source = source.FilterPage(condition.PageCondition);

        return source;
    }
}