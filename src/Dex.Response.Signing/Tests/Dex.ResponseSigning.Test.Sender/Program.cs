using Dex.ResponseSigning.Test.Sender.RefitClients;
using Dex.ResponseSigning.Extensions;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddResponseVerifying(builder.Configuration);

builder.Services.AddHttpClient(
    "TestClient",
    client =>
    {
        client.BaseAddress = new Uri("http://localhost:5678");
    })
    .AddResponseVerifyingHandler();

builder.Services
    .AddRefitClient<IRespondentApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:5678"))
    .AddResponseVerifyingHandler();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();