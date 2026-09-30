using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Dex.Cap.Ef.Tests;

/// <summary>
/// Провайдер логов, который копит записи для проверки в тесте: уровень, категорию, отформатированный текст
/// и структурированные параметры. <see cref="TestLoggerProvider"/> только пишет в вывод теста и проверить
/// запись не даёт.
/// </summary>
internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<LogRecord> _records = new();

    public IReadOnlyList<LogRecord> Records => _records.ToArray();

    public ILogger CreateLogger(string categoryName)
    {
        return new RecordingLogger(categoryName, _records);
    }

    public void Dispose()
    {
    }

    internal sealed record LogRecord(
        LogLevel Level,
        string Category,
        string Message,
        IReadOnlyList<KeyValuePair<string, object?>> State,
        Exception? Exception);

    private sealed class RecordingLogger(string categoryName, ConcurrentQueue<LogRecord> records) : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var structured = state as IEnumerable<KeyValuePair<string, object?>>;
            records.Enqueue(new LogRecord(
                logLevel,
                categoryName,
                formatter(state, exception),
                structured?.ToArray() ?? [],
                exception));
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }
    }
}
