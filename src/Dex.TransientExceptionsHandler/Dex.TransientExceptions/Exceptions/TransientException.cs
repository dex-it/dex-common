namespace Dex.TransientExceptions.Exceptions;

public class TransientException : Exception, ITransientException, ITransientExceptionCandidate
{
    public bool IsTransient => true;

    public TransientException()
    {
    }

    public TransientException(string? message) : base(message)
    {
    }

    public TransientException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}