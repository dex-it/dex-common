using System;

namespace Dex.DataProvider.Contracts
{
    public interface IDataExceptionManager
    {
        Exception Normalize(Exception exception);
        
        bool IsRepeatAction(Exception exception);
    }
}