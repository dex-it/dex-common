using Dex.Audit.Client.Messages;

namespace Dex.Audit.Client.Interfaces;

/// <summary>
/// Интерфейс для управления(конфигурирования и отправки) событиями аудита.
/// </summary>
public interface IAuditWriter
{
    /// <summary>
    /// Обрабатывает событие аудита на основании базовой информации о событии.
    /// </summary>
    /// <param name="eventBaseInfo">Базовая информация о событии аудита.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
    public Task WriteAsync(AuditEventBaseInfo eventBaseInfo, CancellationToken cancellationToken = default);
}
