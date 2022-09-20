using System;

namespace Dex.Ef.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class IndexAttribute : Attribute
    {
        public string? IndexName { get; }

        public int Order { get; }

        public bool IsUnique { get; set; }

        public string? Method { get; set; }

        public IndexAttribute(string? indexName = null, int order = 0)
        {
            IndexName = indexName;
            Order = order;
        }
    }
}