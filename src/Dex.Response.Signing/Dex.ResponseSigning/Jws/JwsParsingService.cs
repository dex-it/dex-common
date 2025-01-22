using System.Text;
using System.Text.Json;
using Dex.ResponseSigning.Serialization;
using Dex.ResponseSigning.Signing;
using Microsoft.AspNetCore.WebUtilities;

namespace Dex.ResponseSigning.Jws;

/// <inheritdoc/>
internal sealed class JwsParsingService : IJwsParsingService
{
    private readonly IVerifySignService _verifySignService;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public JwsParsingService(IVerifySignService verifySignService,
        SigningDataSerializationOptions signingDataSerializationOptions)
    {
        _verifySignService = verifySignService;
        _jsonSerializerOptions = signingDataSerializationOptions.SerializerOptions;
    }

    /// <inheritdoc/>
    public async Task<T> ParseJwsAsync<T>(string jws, CancellationToken cancellationToken)
    {
        var payload = await ParsePayloadAsync(jws, cancellationToken);
        var result = JsonSerializer.Deserialize<T?>(payload, _jsonSerializerOptions);

        if (EqualityComparer<T>.Default.Equals(result, default))
        {
            throw new InvalidOperationException($"Невозможно десериализовать ответ в {typeof(T).Name}.");
        }

        return result ?? throw new InvalidOperationException();
    }

    /// <inheritdoc/>
    public async Task<string> GetSerializedResponseAsync(string jws, CancellationToken cancellationToken)
    {
        var payload = await ParsePayloadAsync(jws, cancellationToken);
        return Encoding.UTF8.GetString(payload);
    }

    private async Task<byte[]> ParsePayloadAsync(string jws, CancellationToken cancellationToken)
    {
        var lastIndexOfDot = jws.LastIndexOf('.');
        var data = jws[..lastIndexOfDot];

        var parts = jws.Split('.');
        var signature = parts[2];

        if (!await _verifySignService.VerifySignAsync(data, signature, cancellationToken))
        {
            throw new InvalidOperationException("Verification failed.");
        }

        var payloadBase64 = parts[1];

        var payload = Base64UrlTextEncoder.Decode(payloadBase64);
        return payload;
    }
}