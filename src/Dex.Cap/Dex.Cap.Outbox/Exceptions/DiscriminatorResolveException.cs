using System;

namespace Dex.Cap.Outbox.Exceptions;

public class DiscriminatorResolveException  : Exception
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