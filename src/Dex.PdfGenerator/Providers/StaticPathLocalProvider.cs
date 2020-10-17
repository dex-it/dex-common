namespace Dex.PdfGenerator.Providers
{
    public class StaticPathLocalProvider : IStaticPathProvider
    {
        private readonly string _sccPath;

        public StaticPathLocalProvider(string sccPath)
        {
            _sccPath = sccPath + "/";
        }
        
        public string GetBaseUri()
        {
            return _sccPath;
        }
    }
}