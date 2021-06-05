using System;
using System.Text.RegularExpressions;

namespace Dex.Lock
{
    public static class InstanceKeyPreparer
    {
        public static string? RemoveSymbols(this string? instanceId)
        {
            if (instanceId == null) return null;
            return Regex.Replace(instanceId, "[^A-Za-z0-9]", "");
        }

        public static string CreateInstanceKey()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}