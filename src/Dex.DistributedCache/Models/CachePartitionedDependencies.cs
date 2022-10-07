namespace Dex.DistributedCache.Models
{
    public class CachePartitionedDependencies
    {
        public string Type { get; }
        public string[] Values { get; }

        public CachePartitionedDependencies(string type, string[] values)
        {
            Type = type;
            Values = values;
        }
    }
}