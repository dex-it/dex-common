using System;
using System.Threading.Tasks;

namespace Dex.PdfGenerator.Providers
{
    public class HtmlStringProvider : IHtmlProvider
    {
        private readonly string _html;
        private readonly IStaticPathProvider _staticPathProvider;

        public HtmlStringProvider(string html, IStaticPathProvider staticPathProvider = null)
        {
            if (string.IsNullOrEmpty(html)) throw new ArgumentException(html);            
            _html = html;
            _staticPathProvider = staticPathProvider;
        }

        public Task<string> GetHtml()
        {
            var result = _staticPathProvider == null ? _html : _html.Replace("{Provider}", _staticPathProvider.GetBaseUri());
            return Task.FromResult(result);
        }
    }
}