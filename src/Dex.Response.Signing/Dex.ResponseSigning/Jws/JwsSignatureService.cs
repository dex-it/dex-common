using System.Text;
using System.Text.Json;
using Dex.ResponseSigning.Options;
using Dex.ResponseSigning.Serialization;
using Dex.ResponseSigning.Signing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Dex.ResponseSigning.Jws;

/// <inheritdoc/>
internal sealed class JwsSignatureService : IJwsSignatureService
{
    private readonly ISignDataService _signDataService;
    private readonly ResponseSigningOptions _responseSigningOptions;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public JwsSignatureService(
        ISignDataService signDataService,
        IOptions<ResponseSigningOptions> responseSigningOptions,
        SigningDataSerializationOptions signingDataSerializationOptions)
    {
        _signDataService = signDataService;
        _responseSigningOptions = responseSigningOptions.Value;
        _jsonSerializerOptions = signingDataSerializationOptions.SerializerOptions;
    }

    /// <inheritdoc/>
    public async Task<string> SignDataAsync(object payload, CancellationToken cancellationToken)
    {
        var headerBytes = Encoding.UTF8.GetBytes($"{{\"alg\":\"{_responseSigningOptions.Algorithm}\"}}");

        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, _jsonSerializerOptions);

        var headerBase64 = Base64UrlTextEncoder.Encode(headerBytes);
        var payloadBase64 = Base64UrlTextEncoder.Encode(payloadBytes);

        var dataToSign = headerBase64 + '.' + payloadBase64;

        var signatureData = await _signDataService.SignDataAsync(dataToSign, cancellationToken);

        return dataToSign + '.' + signatureData;
    }
}