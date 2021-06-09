using System;
using Dex.DataProvider.Exceptions;

namespace Dex.DataProvider.Contracts
{
    public interface IDataExceptionManager
    {
        DataProviderException Normalize(Exception exception);
        
        bool IsRepeatableException(Exception exception);
    }
}