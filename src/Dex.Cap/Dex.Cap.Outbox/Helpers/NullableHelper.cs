using System.Diagnostics.CodeAnalysis;

namespace Dex.Cap.Outbox.Helpers
{
    internal static class NullableHelper
    {
        [return: NotNullIfNotNull("value")]
        public static T? SetNull<T>(ref T? value) where T : class
        {
            var itemRefCopy = value;
            value = null;
            return itemRefCopy;
        }
    }
}
