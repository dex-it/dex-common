using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Dex.ResponseSigning.KeyExtractor;

/// <summary>
/// Базовый сервис для получения приватного ключа RSA
/// </summary>
internal sealed class DefaultPrivateKeyExtractor : IPrivateKeyExtractor
{
    private readonly X509Certificate2 _certificate;

    public DefaultPrivateKeyExtractor(InternalDefaultCertificateExtractor certificateExtractor)
    {
        _certificate = certificateExtractor.Certificate;
    }

    /// <inheritdoc/>
    public RSA GetKey()
    {
        return _certificate.GetRSAPrivateKey() ??
               throw new InvalidOperationException("Не удалось получить приватный ключ");
    }
}