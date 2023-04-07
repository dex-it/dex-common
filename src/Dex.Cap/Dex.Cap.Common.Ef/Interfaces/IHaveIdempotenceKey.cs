namespace Dex.Cap.Common.Ef.Interfaces
{
    public interface IHaveIdempotenceKey
    {
        string IdempotentKey { get; }
    }
}