using System.Text;
using Dex.ResponseSigning.KeyExtractor;
using Microsoft.AspNetCore.WebUtilities;

namespace Dex.ResponseSigning.Signing;

/// <inheritdoc/>
internal sealed class VerifySignService : IVerifySignService
{
    private readonly IPublicKeyExtractor _publicKeyExtractor;

    public VerifySignService(IPublicKeyExtractor publicKeyExtractor)
    {
        _publicKeyExtractor = publicKeyExtractor;
    }

    /// <inheritdoc/>
    public async Task<bool> VerifySignAsync(string data, string signature, CancellationToken cancellationToken)
    {
        var dataBytes = Encoding.UTF8.GetBytes(data);

        var signatureBytes = Base64UrlTextEncoder.Decode(signature);

        using var publicKey = await _publicKeyExtractor.GetKeyAsync(cancellationToken);

        return publicKey.VerifyData(
            dataBytes,
            signatureBytes,
            SigningConstants.HashAlgorithmName,
            SigningConstants.Padding);
    }
}