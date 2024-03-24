using System;

namespace Dex.Cap.Outbox.Exceptions;

public class DiscriminatorResolveTypeException  : Exception
{
    public DiscriminatorResolveTypeException()
    {
    }
    
    public DiscriminatorResolveTypeException(string message) : base(message)
    {
    }
    
    public DiscriminatorResolveTypeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}