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
    public T ParseJws<T>(string jws)
    {
        var payload = ParsePayload(jws);
        var result = JsonSerializer.Deserialize<T?>(payload, _jsonSerializerOptions);

        if (EqualityComparer<T>.Default.Equals(result, default))
        {
            throw new InvalidOperationException($"Невозможно десериализовать ответ в {typeof(T).Name}.");
        }

        return result ?? throw new InvalidOperationException();
    }

    /// <inheritdoc/>
    public string GetSerializedResponse(string jws)
    {
        var payload = ParsePayload(jws);
        return Encoding.UTF8.GetString(payload);
    }

    private byte[] ParsePayload(string jws)
    {
        var lastIndexOfDot = jws.LastIndexOf('.');
        var data = jws[..lastIndexOfDot];

        var parts = jws.Split('.');
        var signature = parts[2];

        if (!_verifySignService.VerifySign(data, signature))
        {
            throw new InvalidOperationException("Verification failed.");
        }

        var payloadBase64 = parts[1];

        var payload = Base64UrlTextEncoder.Decode(payloadBase64);
        return payload;
    }
}