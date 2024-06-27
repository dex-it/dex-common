using Microsoft.Extensions.Logging;

namespace Dex.Audit.Logger.Extensions;

public static class LoggerExtensions
{
    public static void LogAuditDebug(
        this ILogger logger,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Debug, eventType, exception, message, args);
    }

    public static void LogAuditDebug(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Debug, eventType, message, args);
    }

    public static void LogAuditTrace(
        this ILogger logger,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Trace, eventType, exception, message, args);
    }

    public static void LogAuditTrace(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Trace, eventType, message, args);
    }

    public static void LogAuditInformation(
        this ILogger logger,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Information, eventType, exception, message, args);
    }

    public static void LogAuditInformation(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Information, eventType, message, args);
    }

    public static void LogAuditWarning(
        this ILogger logger,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Warning, eventType, exception, message, args);
    }

    public static void LogAuditWarning(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Warning, eventType, message, args);
    }

    public static void LogAuditError(
        this ILogger logger,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Error, eventType, exception, message, args);
    }

    public static void LogAuditError(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Error, eventType, message, args);
    }

    public static void LogAuditCritical(
        this ILogger logger,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Critical, eventType, exception, message, args);
    }

    public static void LogAuditCritical(
        this ILogger logger,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.LogAudit(LogLevel.Critical, eventType, message, args);
    }

    public static void LogAudit(
        this ILogger logger,
        LogLevel logLevel,
        string eventType,
        string? message,
        params object?[] args)
    {
        logger.Log(logLevel, new EventId(int.MaxValue, eventType), null, message, args);
    }

    public static void LogAudit(
        this ILogger logger,
        LogLevel logLevel,
        string eventType,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        logger.Log(logLevel, new EventId(int.MaxValue, eventType), exception, message, args);
    }
}