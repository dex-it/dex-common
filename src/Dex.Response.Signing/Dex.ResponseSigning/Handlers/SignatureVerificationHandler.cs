using System.Text;
using Dex.ResponseSigning.Jws;

namespace Dex.ResponseSigning.Handlers;

/// <summary>
/// Обработчик Http-запросов для верификации ответа от сервиса
/// </summary>
public class SignatureVerificationHandler : DelegatingHandler
{
    private readonly IJwsParsingService _parseJwsService;

    /// <summary>
    /// Конструктор
    /// </summary>
    public SignatureVerificationHandler(IJwsParsingService parseJwsService)
    {
        _parseJwsService = parseJwsService;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return response;
        }

        var mediaType = response.Content.Headers.ContentType;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var serializedResponse = await _parseJwsService.GetSerializedResponseAsync(content, cancellationToken);

        response.Content = new StringContent(serializedResponse, Encoding.UTF8, mediaType?.MediaType);

        return response;
    }
}