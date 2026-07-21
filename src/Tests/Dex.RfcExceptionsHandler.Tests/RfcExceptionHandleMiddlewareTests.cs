using System.Net;
using System.Text.Json;
using Dex.RfcExceptions;
using Dex.RfcExceptionsHandler.Extensions;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;

namespace Dex.RfcExceptionsHandler.Tests;

[TestFixture]
public class RfcExceptionHandleMiddlewareTests
{
    // --- helpers ---

    private static IHost BuildHost(
        RequestDelegate handler,
        string environment = "Development",
        Action<IServiceCollection>? configureServices = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.UseEnvironment(environment);
                web.ConfigureServices(services =>
                {
                    services.AddDefaultRfcExceptionHandleMiddleware();
                    configureServices?.Invoke(services);
                });
                web.Configure(app =>
                {
                    app.UseRfcExceptionHandleMiddleware();
                    app.Run(handler);
                });
            })
            .Build();
    }

    private static async Task<(HttpResponseMessage Response, JsonDocument Body)> SendAsync(
        IHost host, string path = "/test")
    {
        var response = await host.GetTestClient().GetAsync(path);
        var json = await response.Content.ReadAsStringAsync();
        return (response, JsonDocument.Parse(json));
    }

    // --- no-exception passthrough ---

    [Test]
    public async Task NoException_MiddlewareDoesNotInterfere_Returns200()
    {
        using var host = BuildHost(_ => Task.CompletedTask);
        await host.StartAsync();

        var response = await host.GetTestClient().GetAsync("/test");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // --- generic exception ---

    [Test]
    public async Task GenericException_Returns500()
    {
        using var host = BuildHost(_ => throw new InvalidOperationException("boom"));
        await host.StartAsync();

        var (response, _) = await SendAsync(host);

        Assert.That((int)response.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
    }

    [Test]
    public async Task GenericException_ContentType_IsApplicationProblemJson()
    {
        using var host = BuildHost(_ => throw new Exception());
        await host.StartAsync();

        var (response, _) = await SendAsync(host);

        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
    }

    [Test]
    public async Task GenericException_Body_HasExceptionTypeExtension()
    {
        using var host = BuildHost(_ => throw new InvalidOperationException("boom"));
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.GetProperty("exceptionType").GetString(),
            Is.EqualTo("InvalidOperationException"));
    }

    [Test]
    public async Task GenericException_Body_HasTraceIdExtension()
    {
        using var host = BuildHost(_ => throw new Exception());
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.TryGetProperty("traceId", out _), Is.True);
    }

    [Test]
    public async Task GenericException_Body_InstanceIsEncodedPathAndQuery()
    {
        using var host = BuildHost(_ => throw new Exception());
        await host.StartAsync();

        var (_, body) = await SendAsync(host, "/api/orders?id=42");

        Assert.That(body.RootElement.GetProperty("instance").GetString(), Is.EqualTo("/api/orders?id=42"));
    }

    [Test]
    public async Task GenericException_Body_StatusIsSet()
    {
        using var host = BuildHost(_ => throw new Exception());
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.GetProperty("status").GetInt32(),
            Is.EqualTo(StatusCodes.Status500InternalServerError));
    }

    [Test]
    public async Task GenericException_Body_TypeIsResolved()
    {
        using var host = BuildHost(_ => throw new Exception());
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.GetProperty("type").GetString(),
            Is.EqualTo(RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.InternalServerError));
    }

    // --- environment-dependent behaviour ---

    [Test]
    public async Task GenericException_Development_BodyContainsStackTrace()
    {
        using var host = BuildHost(_ => throw new InvalidOperationException("boom"), Environments.Development);
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.TryGetProperty("stackTrace", out _), Is.True);
    }

    [Test]
    public async Task GenericException_Production_BodyDoesNotContainStackTrace()
    {
        using var host = BuildHost(_ => throw new InvalidOperationException("boom"), Environments.Production);
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.TryGetProperty("stackTrace", out _), Is.False);
    }

    [Test]
    public async Task GenericException_Production_DetailDoesNotExposeExceptionMessage()
    {
        const string sensitiveMessage = "secret sql connection string";
        using var host = BuildHost(_ => throw new InvalidOperationException(sensitiveMessage), Environments.Production);
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        var detail = body.RootElement.GetProperty("detail").GetString();
        Assert.That(detail, Does.Not.Contain(sensitiveMessage));
    }

    [Test]
    public async Task GenericException_Development_DetailContainsExceptionMessage()
    {
        const string message = "detailed dev info";
        using var host = BuildHost(_ => throw new InvalidOperationException(message), Environments.Development);
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.GetProperty("detail").GetString(), Is.EqualTo(message));
    }

    // --- IRfcException ---

    [Test]
    public async Task RfcException_HttpStatusCode_ResolvedFromCategory()
    {
        using var host = BuildHost(_ => throw new TestRfcException(ErrorCategory.Conflict));
        await host.StartAsync();
        var (response, _) = await SendAsync(host);
        Assert.That((int)response.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));
    }

    [Test]
    public async Task RfcException_Body_HasTypeFromCategory_TitleDetail()
    {
        using var host = BuildHost(_ => throw new TestRfcException(
            ErrorCategory.NotFound, "Resource not found", "Order 42 not found"));
        await host.StartAsync();
        var (_, body) = await SendAsync(host);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(body.RootElement.GetProperty("type").GetString(), Is.EqualTo(RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.NotFound));
            Assert.That(body.RootElement.GetProperty("title").GetString(), Is.EqualTo("Resource not found"));
            Assert.That(body.RootElement.GetProperty("detail").GetString(), Is.EqualTo("Order 42 not found"));
            Assert.That(body.RootElement.GetProperty("status").GetInt32(), Is.EqualTo(StatusCodes.Status404NotFound));
        }));
    }

    [Test]
    public async Task RfcException_WithErrorCode_TypeIsProblemsSlashCode()
    {
        using var host = BuildHost(_ => throw new TestRfcException(
            ErrorCategory.Conflict, errorCode: "card-has-debt"));
        await host.StartAsync();
        var (_, body) = await SendAsync(host);
        Assert.That(body.RootElement.GetProperty("type").GetString(), Is.EqualTo(RfcErrorCodes.ProblemTypePrefix + "card-has-debt"));
    }

    [Test]
    public async Task RfcException_NullDetail_DetailAbsentFromBody()
    {
        using var host = BuildHost(_ => throw new TestRfcException(ErrorCategory.BadRequest, detail: null));
        await host.StartAsync();
        var (_, body) = await SendAsync(host);
        Assert.That(body.RootElement.TryGetProperty("detail", out _), Is.False);
    }

    [Test]
    public async Task RfcException_WithCustomExtensions_ExtensionsMergedIntoBody()
    {
        var ex = new TestRfcException(ErrorCategory.BadRequest);
        ex.AddExtension("fieldName", "email");
        ex.AddExtension("errorCode", "INVALID_FORMAT");
        using var host = BuildHost(_ => throw ex);
        await host.StartAsync();
        var (_, body) = await SendAsync(host);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(body.RootElement.GetProperty("fieldName").GetString(), Is.EqualTo("email"));
            Assert.That(body.RootElement.GetProperty("errorCode").GetString(), Is.EqualTo("INVALID_FORMAT"));
        }));
    }

    // --- exception.Data ---

    [Test]
    public async Task ExceptionWithData_Body_HasExceptionDataExtension()
    {
        var ex = new Exception("test")
        {
            Data =
            {
                ["orderId"] = 42,
                ["userId"] = "usr-1"
            }
        };

        using var host = BuildHost(_ => throw ex);
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.TryGetProperty("exceptionData", out _), Is.True);
    }

    [Test]
    public async Task ExceptionWithoutData_Body_HasNoExceptionDataExtension()
    {
        using var host = BuildHost(_ => throw new Exception("no data"));
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.TryGetProperty("exceptionData", out _), Is.False);
    }

    // --- custom config ---

    [Test]
    public async Task CustomConfig_Map_TransformsExceptionBeforeProcessing()
    {
        using var host = BuildHost(
            _ => throw new Exception("original"),
            configureServices: services =>
                services.AddRfcExceptionHandleMiddleware<MappingConfig>());
        await host.StartAsync();

        var (response, body) = await SendAsync(host);

        Assert.Multiple((Action)(() =>
        {
            Assert.That((int)response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(body.RootElement.GetProperty("exceptionType").GetString(), Is.EqualTo("ArgumentException"));
        }));
    }

    [Test]
    public async Task OperationCanceledException_Returns499()
    {
        using var host = BuildHost(_ => throw new OperationCanceledException());
        await host.StartAsync();

        var (response, _) = await SendAsync(host);

        Assert.That((int)response.StatusCode, Is.EqualTo(StatusCodes.Status499ClientClosedRequest));
    }

    // --- helpers ---

    private sealed class TestRfcException(
        ErrorCategory category,
        string title = "Test error",
        string? detail = "test detail",
        string? errorCode = null)
        : Exception(detail ?? title), IRfcException
    {
        private readonly Dictionary<string, string> _extensions = new();

        public ErrorCategory Category { get; } = category;
        public string? ErrorCode { get; } = errorCode;
        public string Title { get; } = title;
        public string? Detail { get; } = detail;
        public IReadOnlyDictionary<string, string>? Extensions => _extensions.Count == 0 ? null : _extensions;

        public void AddExtension(string key, string value) => _extensions[key] = value;
    }

    private sealed class MappingConfig : DefaultRfcExceptionHandleConfig
    {
        public override Exception Map(Exception exception) => new ArgumentException("mapped", exception);
    }
}