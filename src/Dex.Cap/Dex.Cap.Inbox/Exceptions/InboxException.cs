using System;

namespace Dex.Cap.Inbox.Exceptions;

public class InboxException : Exception
{
    public InboxException()
    {
    }

    public InboxException(string message) : base(message)
    {
    }

    public InboxException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
