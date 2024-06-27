using System.Text.Json.Serialization;
using Dex.Audit.Client.Extensions;
using Dex.Audit.Client.Messages;
using Dex.Audit.Client.Services;
using Dex.Audit.ClientSample.Repositories;
using Dex.Audit.Logger.Extensions;
using Dex.MassTransit.Rabbit;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.local.json")
    .AddEnvironmentVariables();
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddAuditLogger());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        var enumConverter = new JsonStringEnumConverter();
        opts.JsonSerializerOptions.Converters.Add(enumConverter);
    });
builder.Services.AddAuditClient<BaseAuditEventConfigurator, AuditSettingsRepository>(builder.Configuration);
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(nameof(RabbitMqOptions)));
builder.Services.AddMassTransit(x =>
{
    x.RegisterBus((context, configurator) =>
    {
        context.RegisterSendEndPoint<AuditEventMessage>();
        configurator.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.UseSwagger().UseSwaggerUI();
app.MapControllers();
app.MapGet(
        "/Logger", 
        (
            ILogger<Program> logger,
            LogLevel logLevel,
            string eventType,
            string message,
            string messageParameters
            ) =>
{
    logger.LogAudit(logLevel, eventType, message, messageParameters);
});

app.Run();