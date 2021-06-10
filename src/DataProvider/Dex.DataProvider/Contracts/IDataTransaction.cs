using System;

namespace Dex.DataProvider.Contracts
{
    public interface IDataTransaction : IDisposable
    {
        void Complete();
    }
}