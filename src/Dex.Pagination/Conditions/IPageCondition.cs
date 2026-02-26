namespace Dex.Pagination.Conditions;

public interface IPageCondition
{
    int Page { get; }
    int PageSize { get; }
}