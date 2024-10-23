﻿namespace Dex.Audit.Client.Grpc.Options;

/// <summary>
/// Options for enabling Grpc auditing.
/// </summary>
public class AuditGrpcOptions
{
    /// <summary>
    /// Server address.
    /// </summary>
    public required string ServerAddress { get; init; }
}