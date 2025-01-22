namespace Dex.ResponseSigning.Jws;

/// <summary>
/// Сервис для создания JWS - токена
/// </summary>
public interface IJwsSignatureService
{
    /// <summary>
    /// Подписываем объект
    /// </summary>
    /// <param name="payload">Объект для подписи</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    Task<string> SignDataAsync(object payload, CancellationToken cancellationToken);
}