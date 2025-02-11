using System.Security.Cryptography;

namespace Dex.ResponseSigning.Signing;

/// <summary>
/// Константы
/// </summary>
internal static class SigningConstants
{
    /// <summary>
    /// Алгоритм для хэширования
    /// </summary>
    public static readonly HashAlgorithmName HashAlgorithmName = HashAlgorithmName.SHA256;

    /// <summary>
    /// Настройки <see cref="RSASignaturePadding"/>
    /// </summary>
    public static readonly RSASignaturePadding Padding = RSASignaturePadding.Pkcs1;
}