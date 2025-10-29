namespace Dex.TransientExceptions.Helpers;

internal static class ExceptionExtensions
{
    internal static IEnumerable<Exception> GetInnerExceptions(this Exception exception, int count = 5)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var innerException = exception;
        do
        {
            yield return innerException;

            innerException = innerException.InnerException;
            count--;
        } while (innerException is not null && count > 0);
    }
}