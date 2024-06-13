using Dex.Audit.Writer.Extensions;

var builder = WebApplication.CreateBuilder(args);

var app = builder
    .ConfigureServices()
    .ConfigurePipeline();

app.Run();