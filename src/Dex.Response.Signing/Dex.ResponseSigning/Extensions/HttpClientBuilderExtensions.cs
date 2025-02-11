using Dex.ResponseSigning.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Dex.ResponseSigning.Extensions;

/// <summary>
/// Методы расширений для <see cref="IHttpClientBuilder"/>
/// </summary>
public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// Добавляем в обработчике Http-запросов расшифровку ответа
    /// </summary>
    public static IHttpClientBuilder AddResponseVerifyingHandler(this IHttpClientBuilder builder)
    {
        return builder.AddHttpMessageHandler<SignatureVerificationHandler>();
    }
}