using System.Threading.Tasks;

namespace Dex.PdfGenerator.Providers
{
    public interface IHtmlProvider
    {
        Task<string> GetHtml();
    }
}