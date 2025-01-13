using Microsoft.Extensions.Logging;

namespace Dex.Audit.Logger.Extensions;

/// <summary>
/// An extension for auditable logs.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Auditable log with Debug level.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="exception">Exception.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
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
    /// Auditable log with Debug level.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
    public static void LogAuditDebug(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Debug, eventType, message, args);
    }

    /// <summary>
    /// Auditable log with Trace level.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="exception">Exception.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
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
    /// Auditable log with Trace level.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
    public static void LogAuditTrace(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Trace, eventType, message, args);
    }

    /// <summary>
    /// Auditable log with Information level.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="exception">Exception.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
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
    /// Auditable log with Information level.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
    public static void LogAuditInformation(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Information, eventType, message, args);
    }

    /// <summary>
    /// Auditable log with Warning level.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="exception">Exception.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
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
    /// Auditable log with Warning level.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
    public static void LogAuditWarning(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Warning, eventType, message, args);
    }

    /// <summary>
    /// Auditable log with Error level.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="exception">Exception.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
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
    /// Auditable log with Error level.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
    public static void LogAuditError(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Error, eventType, message, args);
    }

    /// <summary>
    /// Auditable log with Critical level.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="exception">Exception.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
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
    /// Auditable log with Critical level.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
    public static void LogAuditCritical(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Critical, eventType, message, args);
    }

    /// <summary>
    /// Auditable log.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="logLevel">Log level.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
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
    /// Auditable log.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="logLevel">Log level.</param>
    /// <param name="eventType">Event type.</param>
    /// <param name="exception">Exception.</param>
    /// <param name="message">Message.</param>
    /// <param name="args">Message parameters.</param>
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