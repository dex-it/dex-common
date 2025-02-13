using System.Security.Cryptography;

namespace Dex.ResponseSigning.KeyExtractor;

/// <summary>
/// Сервис для получения публичного ключа RSA
/// </summary>
public interface IPublicKeyExtractor
{
    /// <summary>
    /// Получить публичную компоненту RSA - ключа
    /// </summary>
    RSA GetKey();
}