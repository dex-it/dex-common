using System;

namespace Dex.Cap.Inbox.Exceptions;

/// <summary>
/// Базовый тип ошибок инбокса. Наследники уточняют причину.
/// </summary>
public class InboxException : Exception
{
    protected InboxException()
    {
    }

    public InboxException(string message) : base(message)
    {
    }

    protected InboxException(string message, Exception innerException) : base(message, innerException)
    {
    }
}