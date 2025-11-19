using System;
using System.Collections.Generic;

namespace Dex.Extensions
{
    public static class ExceptionToStringExtensions
    {
        public static int Deep { get; set; } = 5;

        public static IEnumerable<string> ExplainToString(this Exception? exception)
        {
            if (exception == null)
            {
                yield return string.Empty;
            }
            else
            {
                var ex = exception;
                for (var i = 0; i < Deep; i++)
                {
                    yield return ex.Message;
                    yield return ex.StackTrace!;
                    ex = ex.InnerException;
                    if (ex == null) break;
                }
            }
        }
    }
}