using Dex.Audit.Domain.Entities;

namespace Dex.Audit.Client.Abstractions.Interfaces;

/// <summary>
/// Репозиторий кэшируемого хранилища.
/// </summary>
public interface IAuditSettingsCacheRepository
{
    /// <summary>
    /// Получить настройки из кэша по типу события.
    /// </summary>
    /// <param name="eventType">Тип события.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>></param>
    /// <returns></returns>
    Task<AuditSettings?> GetAsync(
        string eventType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить настройки из кэша по типам событий.
    /// </summary>
    /// <param name="eventTypes">Типы событий.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>></param>
    /// <returns></returns>
    Task<IDictionary<string, AuditSettings?>> GetDictionaryAsync(
        IEnumerable<string> eventTypes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Добавить в кэш настройки.
    /// </summary>
    /// <param name="settings">Дто настройки события.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>></param>
    /// <returns></returns>
    Task AddRangeAsync(
        IEnumerable<AuditSettings> settings,
        CancellationToken cancellationToken = default);
}