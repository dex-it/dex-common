using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Dex.ResponseSigning.KeyExtractor;

/// <summary>
/// Базовый сервис для получения публичного ключа RSA
/// </summary>
internal sealed class DefaultPublicKeyExtractor : IPublicKeyExtractor
{
    private readonly X509Certificate2 _certificate;

    public DefaultPublicKeyExtractor(InternalDefaultCertificateExtractor certificateExtractor)
    {
        _certificate = certificateExtractor.Certificate;
    }

    /// <inheritdoc/>
    public RSA GetKey()
    {
        return _certificate.GetRSAPublicKey() ??
               throw new InvalidOperationException("Не удалось получить публичный ключ");
    }
}