namespace Dex.RfcExceptionsHandler;

public interface IRfcExceptionHandleConfig
{
    Exception Map(Exception exception);
    int ResolveHttpStatusCode(Exception exception);
}