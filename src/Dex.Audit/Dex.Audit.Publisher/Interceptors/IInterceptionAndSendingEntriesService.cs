using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Dex.Audit.Publisher.Interceptors;

/// <summary>
/// Представляет сервис для перехвата и отправки записей аудита.
/// </summary>
public interface IInterceptionAndSendingEntriesService
{
    /// <summary>
    /// Перехватывает записи аудита из контекста изменений
    /// </summary>
    /// <param name="entries">Коллекция записей аудита</param>
    void InterceptEntries(IEnumerable<EntityEntry> entries);

    /// <summary>
    /// Асинхронно отправляет перехваченные записи аудита
    /// </summary>
    /// <param name="isSuccess">Показатель успешности выполнения операции</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    Task SendInterceptedEntriesAsync(bool isSuccess, CancellationToken cancellationToken = default);
}
