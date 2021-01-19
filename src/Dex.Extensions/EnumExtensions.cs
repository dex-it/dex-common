using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Dex.Extensions
{
    public static class EnumExtensions
    {
        public static T[] GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>().ToArray();
        }

        public static T Parse<T>(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return (T) Enum.Parse(typeof(T), value, true);
        }

        public static bool TryParse<T>(string value, out T obj) where T : struct
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            return Enum.TryParse(value, ignoreCase: true, out obj);
        }
    }
}