using System;

namespace Dex.PdfGenerator.Providers
{
    public class StaticPathUriProvider : IStaticPathProvider
    {
        private readonly Uri _uri;

        public StaticPathUriProvider(Uri uri)
        {
            _uri = uri;
        }
        
        public string GetBaseUri()
        {
            return _uri.ToString();
        }
    }
}