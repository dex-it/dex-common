using System;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.

namespace Dex.Cap.Ef.Tests
{
    internal class TestLoggerProvider : ILoggerProvider
    {
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger();
        }

        private class TestLogger : ILogger
        {
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
            {
                var now = DateTime.Now;
                TestContext.WriteLine($"{now:mm:ss}:{now.Millisecond} [{logLevel}]: {formatter(state, exception!)}");
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                throw new NotImplementedException();
            }
        }
    }
}