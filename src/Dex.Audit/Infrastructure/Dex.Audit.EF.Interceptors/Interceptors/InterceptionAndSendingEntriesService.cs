using System.Text;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Client.Abstractions.Messages;
using Dex.Audit.EF.Interceptors.Abstractions;
using Dex.Audit.EF.Interceptors.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.EF.Interceptors.Interceptors;

/// <summary>
/// A service for intercepting and sending audit records.
/// </summary>
public class InterceptionAndSendingEntriesService(
    IServiceProvider serviceProvider)
    : IInterceptionAndSendingEntriesService
{
    private readonly List<EntryHelper> _entryHelpers = [];

    /// <summary>
    /// Intercepts audit records from the context of changes.
    /// </summary>
    /// <param name="entries">Collection of audit records.</param>
    public void InterceptEntries(IEnumerable<EntityEntry> entries)
    {
        var entityEntries = entries
            .Where(entry =>
                entry.Entity is IAuditEntity &&
                entry.State != EntityState.Unchanged &&
                !_entryHelpers.Exists(entryHelper =>
                    ReferenceEquals(entry.Entity, entryHelper.Entry)));

        foreach (var entry in entityEntries)
        {
            _entryHelpers
                .Add(new EntryHelper(
                    entry.Entity,
                    entry.State,
                    entry.CurrentValues,
                    entry.OriginalValues));
        }
    }

    /// <summary>
    /// Асинхронно отправляет перехваченные записи аудита
    /// </summary>
    /// <param name="isSuccess">Показатель успешности выполнения операции</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public async Task SendInterceptedEntriesAsync(bool isSuccess, CancellationToken cancellationToken = default)
    {
        var auditManager = serviceProvider.GetRequiredService<IAuditWriter>();

        foreach (var entryHelper in _entryHelpers)
        {
            var eventType = GetEventType(entryHelper.State);
            var currentValues = entryHelper.CurrentValues;
            var originalValues = entryHelper.State == EntityState.Added ? currentValues : entryHelper.OriginalValues;
            var message = FormAuditMessage(entryHelper.Entry, eventType, currentValues, originalValues);

            await auditManager.WriteAsync(
                new AuditEventBaseInfo(eventType, entryHelper.Entry.GetType().ToString(), message, isSuccess),
                cancellationToken).ConfigureAwait(false);
        }

        _entryHelpers.Clear();
    }

    protected virtual string GetEventType(EntityState entityState)
    {
        var eventType = entityState switch
        {
            EntityState.Modified => "ObjectChanged",
            EntityState.Added => "ObjectCreated",
            EntityState.Deleted => "ObjectDeleted",
            _ => "None"
        };

        return eventType;
    }

    private static string FormAuditMessage(
        object entity,
        string eventType,
        PropertyValues currentValues,
        PropertyValues originalValues)
    {
        StringBuilder messageBuilder = new();
        messageBuilder.AppendLine($"Event type: {eventType}");
        messageBuilder.AppendLine($"Entity: {entity.GetType()}");
        messageBuilder.AppendLine("Original values:");
        AppendPropertyValues(originalValues, messageBuilder);
        messageBuilder.AppendLine("Current values:");
        AppendPropertyValues(currentValues, messageBuilder);

        return messageBuilder.ToString();
    }

    private static void AppendPropertyValues(
        PropertyValues values,
        StringBuilder messageBuilder)
    {
        foreach (var property in values.Properties)
        {
            messageBuilder.AppendLine($"{property.Name}: {values[property]}");
        }
    }

    /// <summary>
    /// Структура для хранения данных EntityEntry
    /// </summary>
    /// <param name="entry">Объект сущности, информация из которого будет использована для отправки.</param>
    /// <param name="state">EntityState на момент SavingChanges.</param>
    /// <param name="currentValues">Текущие значения свойств сущности.</param>
    /// <param name="originalValues">Исходные значения свойств сущности.</param>
    private readonly struct EntryHelper(
        object entry,
        EntityState state,
        PropertyValues currentValues,
        PropertyValues originalValues)
    {
        /// <summary>
        /// Объект сущности, информация из которого будет использована для отправки.
        /// </summary>
        /// <remarks>Используется в связи с тем, что Id генерируется БД на моменте SavedChanges</remarks>
        public readonly object Entry = entry;

        /// <summary>
        /// EntityState на момент SavingChanges.
        /// </summary>
        public readonly EntityState State = state;

        /// <summary>
        /// Текущие значения свойств сущности.
        /// </summary>
        public readonly PropertyValues CurrentValues = currentValues;

        /// <summary>
        /// Исходные значения свойств сущности.
        /// </summary>
        public readonly PropertyValues OriginalValues = originalValues;
    }
}