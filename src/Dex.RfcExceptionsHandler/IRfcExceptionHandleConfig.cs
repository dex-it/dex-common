using System.Text.Json;

namespace Dex.RfcExceptionsHandler;

/// <summary>
/// RfcExceptionHandleMiddleware config
/// </summary>
public interface IRfcExceptionHandleConfig
{
    JsonSerializerOptions JsonSerializerOptions { get; }

    Exception Map(Exception exception);
    int ResolveHttpStatusCode(Exception exception);
}