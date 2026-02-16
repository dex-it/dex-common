namespace Dex.Pagination.Conditions;

public interface IOrderCondition
{
    string FieldName { get; }
    bool IsDesc { get; }
}