namespace Dex.Audit.Server.Options;

/// <summary>
/// The configuration class of the hosts used by the Client API service.
/// </summary>
public class AuditCacheOptions
{
    /// <summary>
    /// The interval for updating the cache.
    /// </summary>
    public TimeSpan RefreshInterval { get; set; }
}
