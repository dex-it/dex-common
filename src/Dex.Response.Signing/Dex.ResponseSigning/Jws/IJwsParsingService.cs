namespace Dex.ResponseSigning.Jws;

/// <summary>
/// Сервис для расшифровки JWS - токена
/// </summary>
public interface IJwsParsingService
{
    /// <summary>
    /// Расшифровываем JWS - токен
    /// </summary>
    /// <typeparam name="T">Тип ответа</typeparam>
    /// <param name="jws">JWS - токен</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>Типизированный ответ</returns>
    Task<T> ParseJwsAsync<T>(string jws, CancellationToken cancellationToken);

    /// <summary>
    /// Расшифровываем JWS - токен
    /// </summary>
    /// <param name="jws">JWS - токен</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>Ответ в виде json-строки</returns>
    Task<string> GetSerializedResponseAsync(string jws, CancellationToken cancellationToken);
}