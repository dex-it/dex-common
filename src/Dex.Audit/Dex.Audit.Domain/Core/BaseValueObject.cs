using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Dex.Audit.Domain.Core;

/// <summary>
/// Базовый класс для value object'ов
/// </summary>
public abstract class BaseValueObject
{
    /// <summary>
    /// Метод сравения объектов
    /// </summary>
    /// <param name="obj">Объект</param>
    /// <returns>Булево значение результата сравнения</returns>
    /// <remarks>Использует для сравнение метод GetHashCode()</remarks>
    public override bool Equals(object obj)
    {
        if (obj is null)
        {
            return false;
        }

        return GetHashCode() == obj.GetHashCode();
    }

    /// <summary>
    /// Получить хэш кода объекта
    /// </summary>
    /// <returns>Хэш сумма</returns>
    /// <remarks>Реализована при помощи MD5</remarks>
    public override int GetHashCode()
    {
        var serializeObject = JsonSerializer.Serialize(this, GetType()).Trim();
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(serializeObject));
        return BitConverter.ToInt32(hash);
    }
}