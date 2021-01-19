using Dex.Ef.Contracts;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dex.Ef.Provider
{
    internal class DefaultIndexProvider : IIndexProvider
    {
        public IndexBuilder HasMethod(IndexBuilder indexBuilder, string attributeMethod)
        {
            return indexBuilder;
        }
    }
}