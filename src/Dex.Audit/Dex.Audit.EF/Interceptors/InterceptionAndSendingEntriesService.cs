using System.Text;
using Dex.Audit.Client.Interfaces;
using Dex.Audit.Client.Messages;
using Dex.Audit.EF.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.EF.Interceptors;

/// <summary>
/// Сервис для перехвата и отправки записей аудита.
/// </summary>
public class InterceptionAndSendingEntriesService(IServiceProvider serviceProvider)
    : IInterceptionAndSendingEntriesService
{
    private readonly List<EntryHelper> _entryHelpers = new();

    /// <summary>
    /// Перехватывает записи аудита из контекста изменений
    /// </summary>
    /// <param name="entries">Коллекция записей аудита</param>
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

            await auditManager.WriteAsync(new AuditEventBaseInfo(eventType, entryHelper.Entry.GetType().ToString(), message, isSuccess),
                cancellationToken);
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

    private string FormAuditMessage(object entity, string eventType, PropertyValues currentValues, PropertyValues originalValues)
    {
        StringBuilder messageBuilder = new();
        messageBuilder.AppendLine($"Тип события аудита: {eventType}");
        messageBuilder.AppendLine($"Сущность: {entity.GetType()}");
        messageBuilder.AppendLine("Старое значение:");
        AppendPropertyValues(originalValues, messageBuilder);
        messageBuilder.AppendLine("Новое значение:");
        AppendPropertyValues(currentValues, messageBuilder);

        return messageBuilder.ToString();
    }

    private void AppendPropertyValues(PropertyValues values, StringBuilder messageBuilder)
    {
        foreach (var property in values.Properties)
        {
            messageBuilder.AppendLine($"{property.Name}: {values[property]}");
        }
    }
}

/// <summary>
/// Структура для хранения данных EntityEntry
/// </summary>
internal readonly struct EntryHelper
{
    /// <summary>
    /// Объект сущности, информация из которого будет использована для отправки.
    /// </summary>
    /// <remarks>Используется в связи с тем, что Id генерируется БД на моменте SavedChanges</remarks>
    public readonly object Entry;

    /// <summary>
    /// Текущие значения свойств сущности.
    /// </summary>
    public readonly PropertyValues CurrentValues;

    /// <summary>
    /// Исходные значения свойств сущности.
    /// </summary>
    public readonly PropertyValues OriginalValues;

    /// <summary>
    /// EntityState на момент SavingChanges.
    /// </summary>
    public readonly EntityState State;

    public EntryHelper(object entry, EntityState state, PropertyValues currentValues, PropertyValues originalValues)
    {
        Entry = entry;
        State = state;
        CurrentValues = currentValues;
        OriginalValues = originalValues;
    }
}
