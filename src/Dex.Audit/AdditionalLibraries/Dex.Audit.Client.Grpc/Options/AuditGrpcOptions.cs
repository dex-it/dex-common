namespace Dex.Audit.Client.Grpc.Options;

/// <summary>
/// Опции подключения аудита по Grpc.
/// </summary>
public class AuditGrpcOptions
{
    /// <summary>
    /// Адрес сервера.
    /// </summary>
    public required string ServerAddress { get; init; }
}