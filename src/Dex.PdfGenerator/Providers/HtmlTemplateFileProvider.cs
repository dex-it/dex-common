using System.Threading.Tasks;
using RazorLight;

namespace Dex.PdfGenerator.Providers
{
    public class HtmlTemplateFileProvider : IHtmlProvider
    {
        private readonly string _templateDirectoryPath;
        private readonly string _templateFileName;
        private readonly object _model;
        private readonly IStaticPathProvider _staticPathProvider;

        public HtmlTemplateFileProvider(string templateDirectoryPath, string templateFileName, object model, IStaticPathProvider staticPathProvider = null)
        {
            _templateDirectoryPath = templateDirectoryPath;
            _templateFileName = templateFileName;
            _model = model;
            _staticPathProvider = staticPathProvider;
        }

        public async Task<string> GetHtml()
        {
            var engine = new RazorLightEngineBuilder()
                .UseFilesystemProject(_templateDirectoryPath)
                .UseMemoryCachingProvider()
                .Build();

            var html = await engine.CompileRenderAsync(_templateFileName, _model).ConfigureAwait(false);
            return _staticPathProvider == null ? html : html.Replace("{Provider}", _staticPathProvider.GetBaseUri());
        }
    }
}