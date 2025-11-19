using System.Threading.Tasks;
using Dex.PdfGenerator.Providers;
using Dex.PdfGenerator.Settings;

namespace Dex.PdfGenerator
{
    public interface IPdfGenerator
    {
        Task<string> Generate(IHtmlProvider provider, PdfGeneratorSettings? settings = null);
        
        string Generate(string uri, PdfGeneratorSettings? settings = null);
    }
}