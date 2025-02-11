namespace Dex.ResponseSigning.Signing;

/// <summary>
/// Сервис для подписи информации
/// </summary>
public interface ISignDataService
{
    /// <summary>
    /// Подписываем информацию
    /// </summary>
    /// <param name="data">Информация для подписи</param>
    string SignDataAsync(string data);
}