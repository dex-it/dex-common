using System.ComponentModel;

namespace Dex.Audit.Domain.Enums;

/// <summary>
/// Тип события аудита
/// </summary>
public enum AuditEventType
{
    #region События базового аудита

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
    /// Вход пользователя в систему
    /// </summary>
    [Description("Вход пользователя в систему")]
    UserLogin = 3,

    /// <summary>
    /// Выход пользователя из системы
    /// </summary>
    [Description("Выход пользователя из системы")]
    UserLogout = 4,

    /// <summary>
    /// Неуспешный вход в систему
    /// </summary>
    [Description("Неуспешный вход в систему")]
    UserLoginFailed = 5,

    /// <summary>
    /// Создание учетной записи
    /// </summary>
    [Description("Создание учетной записи")]
    AccountCreated = 6,

    /// <summary>
    /// Удаление учетной записи
    /// </summary>
    [Description("Удаление учетной записи")]
    AccountDeleted = 7,

    /// <summary>
    /// Блокировка(отключение) учетной записи
    /// </summary>
    [Description("Блокировка(отключение) учетной записи")]
    AccountBlocked = 8,

    /// <summary>
    /// Разблокировка(включение) учетной записи
    /// </summary>
    [Description("Разблокировка(включение) учетной записи")]
    AccountUnlocked = 9,

    /// <summary>
    /// Назначение\исключение прав пользователя на объект
    /// </summary>
    [Description("Изменение прав пользователя на объект")]
    UserRightsChanged = 10,

    /// <summary>
    /// Смена пароля учетной записи
    /// </summary>
    [Description("Смена пароля учетной записи")]
    PasswordChanged = 11,

    /// <summary>
    /// Создание группы пользователей
    /// </summary>
    [Description("Создание группы пользователей")]
    UserGroupCreated = 12,

    /// <summary>
    /// Создание роли пользователей
    /// </summary>
    [Description("Создание роли пользователей")]
    UserRoleCreated = 13,

    /// <summary>
    /// Удаление группы пользователей
    /// </summary>
    [Description("Удаление группы пользователей")]
    UserGroupDeleted = 14,

    /// <summary>
    /// Удаление роли пользователей
    /// </summary>
    [Description("Удаление роли пользователей")]
    UserRoleDeleted = 15,

    /// <summary>
    /// Изменение прав группы пользователей
    /// </summary>
    [Description("Изменение прав группы пользователей")]
    UserGroupRightsChanged = 16,

    /// <summary>
    /// Изменение прав роли пользователей
    /// </summary>
    [Description("Изменение прав роли пользователей")]
    UserRoleRightsChanged = 17,

    /// <summary>
    /// Исключение пользователя из состава группы
    /// </summary>
    [Description("Исключение пользователя из состава группы")]
    UserExcludedFromGroup = 18,

    /// <summary>
    /// Исключение пользователя из состава роли
    /// </summary>
    [Description("Снятие с пользователя роли")]
    UserExcludedFromRole = 19,

    /// <summary>
    /// Включение пользователя в состав группы
    /// </summary>
    [Description("Включение пользователя в состав группы")]
    UserIncludedToGroup = 20,

    /// <summary>
    /// Включение пользователя в состав роли
    /// </summary>
    [Description("Назначение пользователю роли")]
    UserIncludedToRole = 21,

    /// <summary>
    /// Очистка журнала событий
    /// </summary>
    [Description("Очистка журнала событий")]
    EventLogCleared = 22,

    /// <summary>
    /// Изменения в настройках Аудита
    /// </summary>
    [Description("Изменения в настройках Аудита")]
    AuditSettingsChanged = 23,

    /// <summary>
    /// Изменение конфигурации системы
    /// </summary>
    [Description("Изменение конфигурации системы")]
    SystemConfigurationChanged = 24,

    #endregion

    #region События расширенного аудита

    /// <summary>
    /// Установка прав доступа на объект
    /// </summary>
    [Description("Установка прав доступа на объект")]
    ObjectAccessRightsSet = 25,

    /// <summary>
    /// Изменение прав доступа на объект
    /// </summary>
    [Description("Изменение прав доступа на объект")]
    ObjectAccessRightsChanged = 26,

    /// <summary>
    /// Создание объекта
    /// </summary>
    [Description("Создание объекта")]
    ObjectCreated = 27,

    /// <summary>
    /// Копирование объекта
    /// </summary>
    [Description("Копирование объекта")]
    ObjectCopied = 28,

    /// <summary>
    /// Изменение объекта
    /// </summary>
    [Description("Изменение объекта")]
    ObjectChanged = 29,

    /// <summary>
    /// Чтение объекта
    /// </summary>
    [Description("Чтение объекта")]
    ObjectRead = 30,

    /// <summary>
    /// Удаление объекта.
    /// </summary>
    [Description("Удаление объекта")]
    ObjectDeleted = 31,

    /// <summary>
    /// Архивирование данных
    /// </summary>
    [Description("Архивирование данных")]
    DataArchived = 32,

    /// <summary>
    /// Попытка удаления журналов аудита
    /// </summary>
    [Description("Попытка удаления журналов аудита")]
    TriedDeleteAuditLogs = 33,

    /// <summary>
    /// Отключение пользователя\сессии по тайм-ауту
    /// </summary>
    [Description("Отключение пользователя или сессии по тайм-ауту")]
    SessionTimeoutDisabled = 34,

    /// <summary>
    /// Вход пользователя в подсистему
    /// </summary>
    [Description("Вход пользователя в подсистему")]
    UserSubsystemLogin = 35,

    /// <summary>
    /// Неверное имя при входе в систему
    /// </summary>
    [Description("Неверное имя при входе в систему")]
    InvalidLoginName = 36,

    /// <summary>
    /// Неверный пароль при входе в систему
    /// </summary>
    [Description("Неверный пароль при входе в систему")]
    InvalidPassword = 37,

    /// <summary>
    /// Смена пароля пользователя Администратором системы
    /// </summary>
    [Description("Смена пароля пользователя Администратором системы")]
    UserPasswordChangedByAdmin = 38,

    /// <summary>
    /// Запуск\восстановление подсистемы(компонент, сервисов)
    /// </summary>
    [Description("Запуск подсистемы")]
    StartSubsystem = 39,

    /// <summary>
    /// Остановка\сбой подсистемы(компонент, сервисов)
    /// </summary>
    [Description("Остановка подсистемы")]
    ShutdownSubsystem = 40,

    /// <summary>
    /// События успешного\неуспешного контроля целостности 
    /// </summary>
    [Description("События контроля целостности")]
    EventsIntegrityControl = 41,

    /// <summary>
    /// Неуспешная валидация данных
    /// </summary>
    [Description("Неуспешная валидация данных")]
    DataValidationFailed = 42

    #endregion
}
