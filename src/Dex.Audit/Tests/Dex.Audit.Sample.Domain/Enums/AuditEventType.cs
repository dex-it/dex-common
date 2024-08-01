using System.ComponentModel;

namespace Dex.Audit.Sample.Domain.Enums;

/// <summary>
/// Тип события аудита
/// </summary>
public enum AuditEventType
{
    /// <summary>
    /// Тип события не определен
    /// </summary>
    [Description("Тип события не определен")]
    None = 0,

    /// <summary>
    /// Начало работы (запуск) системы
    /// </summary>
    [Description("Начало работы(запуск) системы")]
    StartSystem = 1,

    /// <summary>
    /// Окончание(остановка) работы системы
    /// </summary>
    [Description("Окончание(остановка) работы системы")]
    ShutdownSystem = 2,

    /// <summary>
    /// Создание объекта
    /// </summary>
    [Description("Создание объекта")]
    ObjectCreated = 3,

    /// <summary>
    /// Изменение объекта
    /// </summary>
    [Description("Изменение объекта")]
    ObjectChanged = 4,

    /// <summary>
    /// Чтение объекта
    /// </summary>
    [Description("Чтение объекта")]
    ObjectRead = 5,

    /// <summary>
    /// Удаление объекта.
    /// </summary>
    [Description("Удаление объекта")]
    ObjectDeleted = 6

}
