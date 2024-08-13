using Microsoft.Extensions.Logging;

namespace Dex.Audit.Logger.Extensions;

/// <summary>
/// Расширение для аудируемых логов.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Аудируемый лог с уровнем Debug.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="exception">Исключение.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAuditDebug(
        this ILogger logger,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Debug, eventType, exception, message, args);
    }

    /// <summary>
    /// Аудируемый лог с уровнем Debug.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAuditDebug(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Debug, eventType, message, args);
    }

    /// <summary>
    /// Аудируемый лог с уровнем Trace.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="exception">Исключение.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAuditTrace(
        this ILogger logger,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Trace, eventType, exception, message, args);
    }

    /// <summary>
    /// Аудируемый лог с уровнем Trace.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAuditTrace(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Trace, eventType, message, args);
    }

    /// <summary>
    /// Аудируемый лог с уровнем Information.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="exception">Исключение.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAuditInformation(
        this ILogger logger,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Information, eventType, exception, message, args);
    }

    /// <summary>
    /// Аудируемый лог с уровнем Information.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAuditInformation(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Information, eventType, message, args);
    }

    /// <summary>
    /// Аудируемый лог с уровнем Warning.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="exception">Исключение.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAuditWarning(
        this ILogger logger,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Warning, eventType, exception, message, args);
    }

    /// <summary>
    /// Аудируемый лог с уровнем Warning.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAuditWarning(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Warning, eventType, message, args);
    }

    /// <summary>
    /// Аудируемый лог с уровнем Error.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="exception">Исключение.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAuditError(
        this ILogger logger,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Error, eventType, exception, message, args);
    }

    /// <summary>
    /// Аудируемый лог с уровнем Error.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAuditError(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Error, eventType, message, args);
    }

    /// <summary>
    /// Аудируемый лог с уровнем Critical.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="exception">Исключение.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAuditCritical(
        this ILogger logger,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Critical, eventType, exception, message, args);
    }

    /// <summary>
    /// Аудируемый лог с уровнем Critical.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAuditCritical(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Critical, eventType, message, args);
    }

    /// <summary>
    /// Аудируемый лог.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="logLevel">Уровень логирования.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAudit(
        this ILogger logger,
        LogLevel logLevel,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.Log(logLevel, new EventId(AuditLoggerConstants.AuditEventId, eventType), null, message, args);
    }

    /// <summary>
    /// Аудируемый лог.
    /// </summary>
    /// <param name="logger">Логгер.</param>
    /// <param name="logLevel">Уровень логирования.</param>
    /// <param name="eventType">Тип события.</param>
    /// <param name="exception">Исключение.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="args">Параметры сообщения.</param>
    public static void LogAudit(
        this ILogger logger,
        LogLevel logLevel,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.Log(logLevel, new EventId(AuditLoggerConstants.AuditEventId, eventType), exception, message, args);
    }
}