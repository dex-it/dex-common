using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Dex.DistributedCache.Models;
using Microsoft.AspNetCore.Http;

#pragma warning disable CA5351

namespace Dex.DistributedCache.Helpers
{
    internal static class CacheHelper
    {
        public static string GenerateETag()
        {
            return DateTimeOffset.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
        }

        public static string GetDisplayUrl(HttpRequest request)
        {
            return Microsoft.AspNetCore.Http.Extensions.UriHelper.GetEncodedPathAndQuery(request);
        }

        public static string CreateMd5(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input); // UTF8 т.к. inputString может содержать любые символы
            var hashBytes = System.Security.Cryptography.MD5.HashData(inputBytes);

            return Convert.ToHexString(hashBytes);
        }

        public static byte[] GetBytes(this CacheMetaInfo cacheMetaInfo)
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cacheMetaInfo));
        }
    }
}