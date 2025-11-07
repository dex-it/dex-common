namespace Dex.Cap.Common.Interfaces;

public interface IIdempotentKey
{
    string IdempotentKey { get; }
}