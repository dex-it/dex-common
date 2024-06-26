using System;
using System.IO;
using System.Threading.Tasks;
using Dex.PdfGenerator.Providers;
using Dex.PdfGenerator.Settings;
using DinkToPdf;

namespace Dex.PdfGenerator
{
    public class PdfGenerator : IPdfGenerator, IDisposable
    {
        private readonly SynchronizedConverter _synchronizedConverter; 
        private PdfTools _pdfTools; 
        private bool _isDisposed;
        
        public PdfGenerator()
        {
            _isDisposed = false;
            _pdfTools = new PdfTools();
            _synchronizedConverter = new SynchronizedConverter(_pdfTools);
        }
        
        public async Task<string> Generate(IHtmlProvider provider, PdfGeneratorSettings settings = null)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            
            var html = await provider.GetHtml().ConfigureAwait(false);
            
            return CreatePdf(html, settings);
        }

        public string Generate(string uri, PdfGeneratorSettings settings = null)
        {
            return CreatePdf(null, settings, uri);
        }

        private string CreatePdf(string html, PdfGeneratorSettings settings, string uri = null)
        {
            var globalSettings = MapSettings(settings);

            var objectSettings = uri == null
                ? new ObjectSettings {HtmlContent = html}
                : new ObjectSettings {Page = uri};

            var doc = new HtmlToPdfDocument
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };
            
            _synchronizedConverter.Convert(doc);
            
            return globalSettings.Out;
        }

        private static GlobalSettings MapSettings(PdfGeneratorSettings settings)
        {
            var globalSettings = new GlobalSettings
            {
                PaperSize = PaperKind.A4,
                DPI = 300,
                Orientation = Orientation.Landscape,
                Out = Guid.NewGuid() + ".pdf",
            };

            if (settings == null) return globalSettings;

            if (settings.PaperWidth > 0 && settings.PaperHeight > 0)
            {
                globalSettings.PaperSize = new PechkinPaperSize(settings.PaperWidth + "mm", settings.PaperHeight + "mm");
            }

            var outFile = settings.SaveFileName ?? Guid.NewGuid() + ".pdf";
            if (!string.IsNullOrEmpty(settings.SaveFilePath))
            {
                outFile = Path.Combine(settings.SaveFilePath, outFile);
            }

            globalSettings.Out = outFile;
            globalSettings.DocumentTitle = settings.DocumentTitle ?? Path.GetFileNameWithoutExtension(outFile); 

            return globalSettings;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing) return;

            if (_pdfTools != null)
            {
               ((IDisposable) _pdfTools).Dispose();
               _pdfTools = null;
            }

            _isDisposed = true;
        }
    }
}