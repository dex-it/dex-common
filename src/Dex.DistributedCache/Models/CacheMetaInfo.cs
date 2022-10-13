namespace Dex.DistributedCache.Models
{
    internal sealed class CacheMetaInfo
    {
        public string ETag { get; }
        public string ContentType { get; }
        public bool IsCompleted { get; private set; }

        public CacheMetaInfo(string eTag, string contentType, bool isCompleted = false)
        {
            ETag = eTag;
            ContentType = contentType;
            IsCompleted = isCompleted;
        }

        public void CompleteCache()
        {
            IsCompleted = true;
        }
    }
}