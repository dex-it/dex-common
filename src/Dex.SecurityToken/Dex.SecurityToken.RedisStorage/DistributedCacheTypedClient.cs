using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Dex.SecurityToken.RedisStorage
{
    public class DistributedCacheTypedClient : IDistributedCacheTypedClient
    {
        private readonly IDistributedCache _distributedCache;

        public DistributedCacheTypedClient(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        }

        public async Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions options, CancellationToken cancellationToken = default)
        {
            var utf8Bytes = JsonSerializer.SerializeToUtf8Bytes(value);

            await _distributedCache.SetAsync(key, utf8Bytes, options, cancellationToken);
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var utf8Bytes = await _distributedCache.GetAsync(key, cancellationToken);

            return Deserialize<T>(utf8Bytes);
        }

        private T? Deserialize<T>(byte[] jsonUtf8Bytes)
        {
            var readOnlySpan = new ReadOnlySpan<byte>(jsonUtf8Bytes);
            var deserializedEntity = JsonSerializer.Deserialize<T>(readOnlySpan);
            return deserializedEntity;
        }
    }
}