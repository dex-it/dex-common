namespace Dex.DistributedCache.Services
{
    public interface ICacheUserVariableFactory
    {
        ICacheUserVariableService GetCacheUserVariableService();
    }
}