using System.Threading.Tasks;

namespace Dex.MassTransit.Sample.Consumer
{
    /// <summary>
    /// Тестовый сервис для получения пароля / токена
    /// </summary>
    public interface ITestPasswordService
    {
        Task<string> GetAccessToken();
    }
}