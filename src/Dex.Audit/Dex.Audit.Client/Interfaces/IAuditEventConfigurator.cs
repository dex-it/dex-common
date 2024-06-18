using Dex.Audit.Client.Messages;

namespace Dex.Audit.Client.Interfaces;

/// <summary>
/// Интерфейс для конфигурации событий аудита
/// </summary>
public interface IAuditEventConfigurator
{
    /// <summary>
    /// Конфигурирует событие аудита на основе переданной базовой информации о событии
    /// </summary>
    /// <param name="auditEventBaseInfo">Базовая информация о событии аудита</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public Task<AuditEventMessage> ConfigureAuditEventAsync(AuditEventBaseInfo auditEventBaseInfo, CancellationToken cancellationToken = default);
}
