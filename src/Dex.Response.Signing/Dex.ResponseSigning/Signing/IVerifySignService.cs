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
    bool VerifySign(string data, string signature);
}