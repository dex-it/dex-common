using System.Text;
using Dex.Audit.Contracts.Interfaces;
using Dex.Audit.Contracts.Messages;
using Dex.Audit.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.Audit.Publisher.Interceptors;

/// <summary>
/// Сервис для перехвата и отправки записей аудита.
/// </summary>
internal sealed class InterceptionAndSendingEntriesService : IInterceptionAndSendingEntriesService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<EntryHelper> _entryHelpers = new();

    public InterceptionAndSendingEntriesService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Перехватывает записи аудита из контекста изменений
    /// </summary>
    /// <param name="entries">Коллекция записей аудита</param>
    public void InterceptEntries(IEnumerable<EntityEntry> entries)
    {
        IEnumerable<EntityEntry> entityEntries = entries
            .Where(entry =>
                entry.Entity is IAuditEntity &&
                entry.State != EntityState.Unchanged &&
                !_entryHelpers.Exists(entryHelper =>
                    ReferenceEquals(entry.Entity, entryHelper.Entry)));

        foreach (EntityEntry entry in entityEntries)
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
        IAuditManager auditManager = _serviceProvider.GetRequiredService<IAuditManager>();
            
        foreach (EntryHelper entryHelper in _entryHelpers)
        {
            AuditEventType eventType = GetEventType(entryHelper.State);
            PropertyValues currentValues = entryHelper.CurrentValues;
            PropertyValues originalValues = entryHelper.State == EntityState.Added ? currentValues : entryHelper.OriginalValues;
            string message = FormAuditMessage(entryHelper.Entry, eventType, currentValues, originalValues);

            await auditManager.ProcessAuditEventAsync(new AuditEventBaseInfo(eventType, entryHelper.Entry.GetType().ToString(), message, isSuccess),
                cancellationToken);
        }

        _entryHelpers.Clear();
    }

    private AuditEventType GetEventType(EntityState entityState)
    {
        AuditEventType eventType = entityState switch
        {
            EntityState.Modified => AuditEventType.ObjectChanged,
            EntityState.Added => AuditEventType.ObjectCreated,
            EntityState.Deleted => AuditEventType.ObjectDeleted,
            _ => AuditEventType.None
        };

        return eventType;
    }

    private string FormAuditMessage(object entity, AuditEventType eventType, PropertyValues currentValues, PropertyValues originalValues)
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
        foreach (IProperty property in values.Properties)
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
    /// Объект сущности, информация из которого будет использована для отправки
    /// </summary>
    /// <remarks>Используется в связи с тем, что Id генерируется БД на моменте SavedChanges</remarks>
    public readonly object Entry;

    /// <summary>
    /// Текущие значения свойств сущности
    /// </summary>
    public readonly PropertyValues CurrentValues;

    /// <summary>
    /// Исходные значения свойств сущности
    /// </summary>
    public readonly PropertyValues OriginalValues;

    /// <summary>
    /// EntityState на момент SavingChanges
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
