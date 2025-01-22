using System.Text;
using Dex.ResponseSigning.KeyExtractor;
using Microsoft.AspNetCore.WebUtilities;

namespace Dex.ResponseSigning.Signing;

/// <inheritdoc/>
internal sealed class SignDataService : ISignDataService
{
    private readonly IPrivateKeyExtractor _privateKeyExtractor;

    public SignDataService(IPrivateKeyExtractor privateKeyExtractor)
    {
        _privateKeyExtractor = privateKeyExtractor;
    }

    /// <inheritdoc/>
    public async Task<string> SignDataAsync(string data, CancellationToken cancellationToken)
    {
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var privateKey = await _privateKeyExtractor.GetKeyAsync(cancellationToken);

        var signedBytes = privateKey.SignData(
            dataBytes,
            SigningConstants.HashAlgorithmName,
            SigningConstants.Padding);

        return Base64UrlTextEncoder.Encode(signedBytes);
    }
}