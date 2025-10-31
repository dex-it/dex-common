namespace Dex.TransientExceptions.Exceptions;

public interface ITransientExceptionCandidate
{
    bool IsTransient { get; }
}