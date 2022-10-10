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
            return Microsoft.AspNetCore.Http.Extensions.UriHelper.GetDisplayUrl(request);
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        // ReSharper disable once UnusedMember.Local
        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static string CreateMd5(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes);
        }

        public static byte[] GetBytes(this CacheMetaInfo cacheMetaInfo)
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cacheMetaInfo));
        }
    }
}