namespace Dex.Cap.OnceExecutor.Ef
{
    public interface IOnceExecutorEf<out TDbContext, T> : IOnceExecutor<TDbContext, T>
    {
    }
}