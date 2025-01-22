using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Dex.ResponseSigning.Options;
using Microsoft.Extensions.Options;

namespace Dex.ResponseSigning.KeyExtractor;

/// <summary>
/// Внутренний сервис по извлечению <see cref="RSA"/>
/// </summary>
internal sealed class InternalDefaultCertificateExtractor
{
    private const string CertificateResourceName = "Dex.ResponseSigning.Certificate.ResponseSigning.pfx";

    private readonly X509Certificate2 _certificate;

    public X509Certificate2 Certificate => _certificate;

    public InternalDefaultCertificateExtractor(IOptions<ResponseSigningOptions> responseSigningOptions)
    {
        using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(CertificateResourceName)
                                   ?? throw new FileNotFoundException(CertificateResourceName);

        var raw = new byte[resourceStream.Length];

        for (var i = 0; i < resourceStream.Length; ++i)
        {
            raw[i] = (byte)resourceStream.ReadByte();
        }

        _certificate = new X509Certificate2(raw, responseSigningOptions.Value.DefaultPassword);
    }
}