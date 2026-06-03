using System;
using System.Collections.Generic;

namespace Dex.Extensions
{
    public static class ExceptionToStringExtensions
    {
        public static IEnumerable<string> ExplainToString(this Exception? exception, int deep = 5)
        {
            if (exception == null)
            {
                yield return string.Empty;
            }
            else
            {
                var ex = exception;
                for (var i = 0; i < deep; i++)
                {
                    yield return ex.Message;
                    yield return ex.StackTrace;
                    ex = ex.InnerException;
                    if (ex == null) break;
                }
            }
        }


        public static IEnumerable<Exception> GetInnerExceptions(this Exception exception, int deep = 5)
        {
#if NET8_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(exception);
#endif

            if (deep <= 0) yield break;

            var innerException = exception;
            do
            {
                yield return innerException;

                innerException = innerException.InnerException;
                deep--;
            } while (innerException != null && deep > 0);
        }
    }
}