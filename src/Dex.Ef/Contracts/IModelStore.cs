using System;
using System.Collections.Generic;

namespace Dex.Ef.Contracts
{
    public interface IModelStore
    {
        IEnumerable<Type> GetModels();
    }
}