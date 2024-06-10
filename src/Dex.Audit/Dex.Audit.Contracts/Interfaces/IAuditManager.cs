using Dex.Audit.Contracts.Messages;

namespace Dex.Audit.Contracts.Interfaces;

/// <summary>
/// Интерфейс для управления(конфигурирования и отправки) событиями аудита
/// </summary>
public interface IAuditManager
{
    /// <summary>
    /// Обрабатывает событие аудита на основании базовой информации о событии
    /// </summary>
    /// <param name="eventBaseInfo">Базовая информация о событии аудита</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public Task ProcessAuditEventAsync(AuditEventBaseInfo eventBaseInfo, CancellationToken cancellationToken = default);
}
