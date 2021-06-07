using System;
using System.Collections.Generic;

namespace Dex.DataProvider.Ef.Contracts
{
    public interface IModelStore
    {
        IEnumerable<Type> GetModels();
    }
}