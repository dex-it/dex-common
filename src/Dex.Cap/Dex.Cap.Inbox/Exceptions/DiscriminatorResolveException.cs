using System;

namespace Dex.Cap.Inbox.Exceptions;

/// <summary>
/// Не удалось сопоставить дискриминатор сохранённого сообщения с типом.
/// </summary>
public class DiscriminatorResolveException : InboxException
{
    public DiscriminatorResolveException()
    {
    }

    public DiscriminatorResolveException(string message) : base(message)
    {
    }

    public DiscriminatorResolveException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
