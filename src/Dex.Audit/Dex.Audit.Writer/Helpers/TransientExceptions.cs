using System.Net.Sockets;
using Npgsql;

namespace Dex.Audit.Writer.Helpers;

/// <summary>
/// Класс - помощник для работы с транзиентными ошибками
/// </summary>
public static class TransientExceptions
{
    private static readonly HashSet<Type> _transientExceptions = new()
    {
        typeof(HttpRequestException),
        typeof(TimeoutException),
        typeof(IOException),
        typeof(SocketException)
    };

    /// <summary>
    /// Указываем дополнительные исключения, которые будем считать тарнзиентными
    /// </summary>
    public static HashSet<Type> Add(IEnumerable<Type> exceptions)
        => _transientExceptions.Intersect(exceptions).ToHashSet();

    /// <summary>
    /// Проверяем, является ли ошибка транзиентной
    /// </summary>
    public static bool Check(Exception exception)
    {
        if (exception is NpgsqlException npgsqlException)
        {
            return npgsqlException.IsTransient;
        }

        return _transientExceptions.Any(x => x.IsInstanceOfType(exception));
    }
}
