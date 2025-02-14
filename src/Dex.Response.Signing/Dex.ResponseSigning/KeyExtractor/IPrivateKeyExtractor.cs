using System.Security.Cryptography;

namespace Dex.ResponseSigning.KeyExtractor;

/// <summary>
/// Сервис для получения приватного ключа RSA
/// </summary>
public interface IPrivateKeyExtractor
{
    /// <summary>
    /// Получить приватную компоненту RSA - ключа
    /// </summary>
    RSA GetKey();
}