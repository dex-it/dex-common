using System;
using System.Threading.Tasks;
using RazorLight;

namespace Dex.PdfGenerator.Providers
{
    public class HtmlTemplateStringProvider : IHtmlProvider
    {
        private readonly string _templateString;
        private readonly string _templateKey;
        private readonly object _model;
        private readonly IStaticPathProvider _staticPathProvider;

        public HtmlTemplateStringProvider(string templateString, string templateKey, object model, IStaticPathProvider staticPathProvider = null)
        {
            _templateString = templateString;
            _templateKey = templateKey;
            _model = model;
            _staticPathProvider = staticPathProvider;
        }

        public async Task<string> GetHtml()
        {
            var engine = new RazorLightEngineBuilder()
                .UseMemoryCachingProvider()
                .Build();

            var html = await engine.CompileRenderAsync(_templateKey ?? Guid.NewGuid().ToString(), _templateString, _model);
            return _staticPathProvider == null ? html : html.Replace("{Provider}", _staticPathProvider.GetBaseUri());
        }
    }
}