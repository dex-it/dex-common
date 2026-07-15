using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dex.Cap.Inbox.Interfaces;

/// <summary>
/// Удаление обработанных сообщений из хранилища.
/// </summary>
public interface IInboxCleanupDataProvider
{
    /// <summary>
    /// Удалить завершённые сообщения старше указанного возраста.
    /// </summary>
    /// <remarks>
    /// Ретеншен обработанных сообщений это одновременно и окно дедупликации: удалив запись,
    /// система перестаёт распознавать повторную доставку этого сообщения.
    /// </remarks>
    /// <returns>Количество удалённых сообщений.</returns>
    Task<int> Cleanup(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
