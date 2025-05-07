namespace Dex.Audit.Client.Implementations.Grpc.Options;

/// <summary>
/// Options for enabling Grpc auditing.
/// </summary>
public sealed class AuditGrpcOptions
{
    /// <summary>
    /// Server address.
    /// </summary>
    public required string ServerAddress { get; init; }
}