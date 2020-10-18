using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Dex.Extensions
{
    public static class StringExtension
    {
        public static string ReplaceRegex(this string source, string pattern, string replacement)
        {
            return Regex.Replace(source, pattern, replacement);
        }

        public static bool IsNullOrEmpty(this string source)
        {
            return string.IsNullOrEmpty(source);
        }

        public static bool IsNullOrWhiteSpace(this string source)
        {
            return string.IsNullOrWhiteSpace(source);
        }

        public static byte[] GetMd5Hash(this string value, Encoding encoding)
        {
            using var md5 = MD5.Create();
            return md5.ComputeHash(encoding.GetBytes(value));
        }

        public static byte[] GetSha256Hash(this string value, Encoding encoding)
        {
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(encoding.GetBytes(value));
        }

        public static byte[] GetSha1Hash(this string value, Encoding encoding)
        {
            using var sha1 = SHA1.Create();
            return sha1.ComputeHash(encoding.GetBytes(value));
        }

        public static string GetMd5HashString(this string value, Encoding encoding)
        {
            return GetStringFromHash(GetMd5Hash(value, encoding));
        }

        public static string GetSha256HashString(this string value, Encoding encoding)
        {
            return GetStringFromHash(GetSha256Hash(value, encoding));
        }

        public static string GetSha1HashString(this string value, Encoding encoding)
        {
            return GetStringFromHash(GetSha1Hash(value, encoding));
        }

        public static string GetMd5HashString(this string value)
        {
            return GetMd5HashString(value, Encoding.UTF8);
        }

        public static string GetSha256HashString(this string value)
        {
            return GetSha256HashString(value, Encoding.UTF8);
        }

        public static string GetSha1HashString(this string value)
        {
            return GetSha1HashString(value, Encoding.UTF8);
        }

        private static string GetStringFromHash(IEnumerable<byte> hash)
        {
            var sb = new StringBuilder();

            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}