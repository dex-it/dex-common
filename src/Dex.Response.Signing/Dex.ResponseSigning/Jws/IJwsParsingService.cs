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
    /// <returns>Типизированный ответ</returns>
    T ParseJws<T>(string jws);

    /// <summary>
    /// Расшифровываем JWS - токен
    /// </summary>
    /// <param name="jws">JWS - токен</param>
    /// <returns>Ответ в виде json-строки</returns>
    string GetSerializedResponse(string jws);
}