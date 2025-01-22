namespace Dex.ResponseSigning.Signing;

/// <summary>
/// Сервис для верификации подписанной информации
/// </summary>
public interface IVerifySignService
{
    /// <summary>
    /// Верифицируем подписанную информацию
    /// </summary>
    /// <param name="data">Подписанную информацию</param>
    /// <param name="signature">Подпись для верификации</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    Task<bool> VerifySignAsync(string data, string signature, CancellationToken cancellationToken);
}