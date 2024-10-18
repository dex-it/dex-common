using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Dex.Audit.EF.Interceptors.Interfaces;

/// <summary>
/// Represents a service for intercepting and sending audit records.
/// </summary>
public interface IInterceptionAndSendingEntriesService
{
    /// <summary>
    /// Intercepts audit records from the context of changes.
    /// </summary>
    /// <param name="entries">Collection of audit records.</param>
    void InterceptEntries(IEnumerable<EntityEntry> entries);

    /// <summary>
    /// Asynchronously sends intercepted audit records
    /// </summary>
    /// <param name="isSuccess">The success rate of the operation.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    Task SendInterceptedEntriesAsync(bool isSuccess, CancellationToken cancellationToken = default);
}
