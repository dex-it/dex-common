namespace Dex.PdfGenerator.Settings
{
    public class PdfGeneratorSettings
    {
        public string DocumentTitle { get; set; } 
        public string SaveFilePath { get; set; }
        public string SaveFileName { get; set; }
        public FileOrientation? PaperOrientation { get; set; }
        public uint PaperHeight { get; set; }
        public uint PaperWidth { get; set; }
    }
}