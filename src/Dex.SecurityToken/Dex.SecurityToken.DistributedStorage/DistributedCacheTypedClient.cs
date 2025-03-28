using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Dex.SecurityToken.DistributedStorage
{
    internal class DistributedCacheTypedClient : IDistributedCacheTypedClient
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

        private static T? Deserialize<T>(byte[]? jsonUtf8Bytes)
        {
            if (jsonUtf8Bytes == null || jsonUtf8Bytes.Length == 0)
            {
                return default;
            }

            var readOnlySpan = new ReadOnlySpan<byte>(jsonUtf8Bytes);
            var deserializedEntity = JsonSerializer.Deserialize<T>(readOnlySpan);
            return deserializedEntity;
        }
    }
}