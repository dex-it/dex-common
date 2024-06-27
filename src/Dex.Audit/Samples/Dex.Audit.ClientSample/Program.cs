using Dex.Audit.Logger.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddAuditLogger());
var app = builder.Build();

app.MapGet("/Logger", (ILogger<Program> logger) =>
{
    logger.LogAuditDebug("EventType", "Message with {0}", "params");
});

app.Run();