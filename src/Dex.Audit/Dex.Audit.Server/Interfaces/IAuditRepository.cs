using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Server.Interfaces;

public interface IAuditRepository
{
    Task AddAuditEventAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить все настройки 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<AuditSettings>> GetAllSettingsAsync(CancellationToken cancellationToken);
}