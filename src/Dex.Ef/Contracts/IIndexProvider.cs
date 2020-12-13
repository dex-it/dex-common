using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dex.Ef.Contracts
{
    public interface IIndexProvider
    {
        IndexBuilder HasMethod(IndexBuilder indexBuilder, string attributeMethod);
    }
}