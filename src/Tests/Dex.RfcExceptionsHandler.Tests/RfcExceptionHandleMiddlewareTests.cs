using System.Net;
using System.Text.Json;
using Dex.RfcAbstractions;
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
            Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + RfcErrorCodes.InternalServerError));
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
            Assert.That(body.RootElement.GetProperty("type").GetString(), Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + RfcErrorCodes.NotFound));
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
        Assert.That(body.RootElement.GetProperty("type").GetString(), Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + "card-has-debt"));
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
    public async Task OperationCanceledException_Returns499_WithUnknownType()
    {
        using var host = BuildHost(_ => throw new OperationCanceledException());
        await host.StartAsync();

        var (response, body) = await SendAsync(host);

        Assert.Multiple((Action)(() =>
        {
            // 499 нет в таблице статусов => fallback-код "unknown"
            Assert.That((int)response.StatusCode, Is.EqualTo(StatusCodes.Status499ClientClosedRequest));
            Assert.That(body.RootElement.GetProperty("type").GetString(),
                Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + RfcErrorCodes.Unknown));
        }));
    }

    [Test]
    public async Task Status418_Fallback_ReturnsImATeapotType()
    {
        // конфиг мапит любое не-IRfcException исключение в 418
        using var host = BuildHost(_ => throw new InvalidOperationException("teapot"),
            configureServices: services =>
                services.AddSingleton<IRfcExceptionHandleConfig>(new TeapotConfig()));
        await host.StartAsync();

        var (response, body) = await SendAsync(host);

        Assert.Multiple((Action)(() =>
        {
            Assert.That((int)response.StatusCode, Is.EqualTo(StatusCodes.Status418ImATeapot));
            Assert.That(body.RootElement.GetProperty("type").GetString(),
                Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + RfcErrorCodes.ImATeapot));
        }));
    }

    // --- минимальный реализатор (ErrorCode объявлен явно и возвращает null; Extensions — null) ---

    [Test]
    public async Task MinimalRfcException_UsesCategoryCode_NoCustomExtensions()
    {
        using var host = BuildHost(_ => throw new MinimalRfcException(ErrorCategory.NotFound));
        await host.StartAsync();

        var (response, body) = await SendAsync(host);

        Assert.Multiple((Action)(() =>
        {
            Assert.That((int)response.StatusCode, Is.EqualTo(StatusCodes.Status404NotFound));
            // ErrorCode не задан => type по категории
            Assert.That(body.RootElement.GetProperty("type").GetString(),
                Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + RfcErrorCodes.NotFound));
            // кастомных extensions нет — только служебные (exceptionType/traceId)
            Assert.That(body.RootElement.TryGetProperty("custom", out _), Is.False);
        }));
    }

    // --- нормализация ErrorCode (#4) ---

    [Test]
    public async Task ErrorCode_Empty_FallsBackToCategoryCode()
    {
        using var host = BuildHost(_ => throw new TestRfcException(
            ErrorCategory.Conflict, errorCode: "   "));
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.GetProperty("type").GetString(),
            Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + RfcErrorCodes.Conflict));
    }

    // Миграционная ловушка: перенос полного URI из 8.0.1 RfcType в ErrorCode.
    // Категория Conflict, а код not-found — так тест отличает "префикс снят"
    // от "код выродился и сработал fallback на код категории".
    // Разный регистр префикса проверяет, что срез регистронезависимый.
    [TestCase("/problems/not-found", "not-found")]
    [TestCase("/PROBLEMS/not-found", "not-found")]
    [TestCase("/Problems/not-found", "not-found")]
    public async Task ErrorCode_WithProblemsPrefix_IsStripped_NoDoublePrefix(string errorCode, string expected)
    {
        using var host = BuildHost(_ => throw new TestRfcException(
            ErrorCategory.Conflict, errorCode: errorCode));
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.GetProperty("type").GetString(),
            Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + expected));
    }

    [TestCase("error-404", "error-404")]
    [TestCase("conflict/sub-2", "conflict/sub-2")]
    public async Task ErrorCode_WithDigits_IsPreserved(string errorCode, string expected)
    {
        using var host = BuildHost(_ => throw new TestRfcException(
            ErrorCategory.Conflict, errorCode: errorCode));
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.GetProperty("type").GetString(),
            Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + expected));
    }

    [Test]
    public async Task ErrorCode_WithLeadingSlash_IsTrimmed()
    {
        using var host = BuildHost(_ => throw new TestRfcException(
            ErrorCategory.BadRequest, errorCode: "/custom-code"));
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.GetProperty("type").GetString(),
            Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + "custom-code"));
    }

    [TestCase("/problems/")]
    [TestCase("///")]
    [TestCase("/problems///")]
    public async Task ErrorCode_DegenerateAfterNormalization_FallsBackToCategoryCode(string degenerate)
    {
        // код, который непуст ДО нормализации, но пуст ПОСЛЕ — не должен давать битый "/problems/"
        using var host = BuildHost(_ => throw new TestRfcException(
            ErrorCategory.NotFound, errorCode: degenerate));
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.GetProperty("type").GetString(),
            Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + RfcErrorCodes.NotFound));
    }

    [TestCase("../../etc/passwd")]
    [TestCase("with space")]
    [TestCase("Card-Has-Debt")]
    [TestCase("a//b")]
    [TestCase("code!")]
    public async Task ErrorCode_InvalidFormat_FallsBackToCategoryCode(string invalid)
    {
        // код не в формате lowercase-kebab => отбрасывается, type по категории (валидный URI)
        using var host = BuildHost(_ => throw new TestRfcException(
            ErrorCategory.Conflict, errorCode: invalid));
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.GetProperty("type").GetString(),
            Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + RfcErrorCodes.Conflict));
    }

    [Test]
    public async Task ErrorCode_MultiSegmentKebab_IsPreserved()
    {
        using var host = BuildHost(_ => throw new TestRfcException(
            ErrorCategory.Conflict, errorCode: "conflict/card-has-debt"));
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.GetProperty("type").GetString(),
            Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + "conflict/card-has-debt"));
    }

    // --- DIM-ловушка Extensions устранена (#6) ---

    [Test]
    public async Task DerivedException_ExtensionsFromDerived_AreNotLost()
    {
        // Extensions объявлены только в наследнике, IRfcException — на базе.
        // После перевода Extensions в обычный член значения не теряются.
        using var host = BuildHost(_ => throw new DerivedWithExtensionsException(ErrorCategory.Conflict));
        await host.StartAsync();

        var (_, body) = await SendAsync(host);

        Assert.That(body.RootElement.GetProperty("custom").GetString(), Is.EqualTo("derived-value"));
    }

    // --- reserved-ключи в custom Extensions не перезаписываются (#A) ---

    [Test]
    public async Task CustomExtensions_ReservedRfcKeys_AreNotOverwritten()
    {
        var ex = new TestRfcException(ErrorCategory.Conflict, errorCode: "card-has-debt");
        // попытка инъекции зарезервированных RFC 9457 членов через Extensions
        ex.AddExtension("type", "javascript:alert(1)");
        ex.AddExtension("status", "200");
        ex.AddExtension("detail", "injected");
        ex.AddExtension("instance", "/injected");
        ex.AddExtension("title", "injected title");
        // и служебного ключа middleware
        ex.AddExtension("exceptionType", "Fake");
        // легитимный кастомный ключ должен пройти
        ex.AddExtension("customField", "ok");

        using var host = BuildHost(_ => throw ex);
        await host.StartAsync();

        var (response, body) = await SendAsync(host);
        var root = body.RootElement;

        Assert.Multiple((Action)(() =>
        {
            // reserved-ключи сохранили значения middleware, а не инъекцию
            Assert.That((int)response.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));
            Assert.That(root.GetProperty("type").GetString(),
                Is.EqualTo(RfcTypeConstants.ProblemTypePrefix + "card-has-debt"));
            Assert.That(root.GetProperty("status").GetInt32(), Is.EqualTo(StatusCodes.Status409Conflict));
            Assert.That(root.GetProperty("detail").GetString(), Is.Not.EqualTo("injected"));
            Assert.That(root.GetProperty("exceptionType").GetString(), Is.Not.EqualTo("Fake"));
            // легитимный ключ прошёл
            Assert.That(root.GetProperty("customField").GetString(), Is.EqualTo("ok"));
        }));
    }

    // --- уровень логирования по статусу (#4) ---

    [Test]
    public async Task Status5xx_LogsError_WithException()
    {
        var logs = new FakeLogCollector();
        using var host = BuildHost(_ => throw new TestRfcException(ErrorCategory.IntegrationError),
            configureServices: services => services.AddSingleton<ILoggerProvider>(new FakeLoggerProvider(logs)));
        await host.StartAsync();

        await SendAsync(host);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(logs.Entries.Count(e => e.Level == LogLevel.Error), Is.EqualTo(1));
            Assert.That(logs.Entries.Any(e => e.Level == LogLevel.Error && e.HasException), Is.True);
            Assert.That(logs.Entries.Any(e => e.Level == LogLevel.Warning), Is.False);
        }));
    }

    [Test]
    public async Task Status4xx_LogsWarning_NoError()
    {
        var logs = new FakeLogCollector();
        using var host = BuildHost(_ => throw new TestRfcException(ErrorCategory.Conflict),
            configureServices: services => services.AddSingleton<ILoggerProvider>(new FakeLoggerProvider(logs)));
        await host.StartAsync();

        await SendAsync(host);

        Assert.Multiple((Action)(() =>
        {
            Assert.That(logs.Entries.Any(e => e.Level == LogLevel.Warning), Is.True);
            Assert.That(logs.Entries.Any(e => e.Level == LogLevel.Error), Is.False);
        }));
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

    /// <summary>
    /// Минимальный реализатор контракта: только обязательные члены.
    /// ErrorCode и Extensions объявлены явно и возвращают null
    /// (оба — обычные члены интерфейса, не DIM).
    /// </summary>
    private sealed class MinimalRfcException(ErrorCategory category)
        : Exception("minimal"), IRfcException
    {
        public ErrorCategory Category { get; } = category;
        public string? ErrorCode => null;
        public string Title => "Minimal";
        public string Detail => "minimal detail";
        public IReadOnlyDictionary<string, string>? Extensions => null;
    }

    /// <summary>
    /// Базовое доменное исключение, реализующее IRfcException.
    /// </summary>
    private abstract class BaseDomainException(ErrorCategory category)
        : Exception("domain"), IRfcException
    {
        public ErrorCategory Category { get; } = category;
        public virtual string? ErrorCode => null;
        public string Title => "Domain";
        public string Detail => "domain detail";
        public virtual IReadOnlyDictionary<string, string>? Extensions => null;
    }

    /// <summary>
    /// Наследник, добавляющий Extensions поверх базы. Проверяет, что после перевода
    /// Extensions в обычный член (не DIM) значения наследника НЕ теряются при
    /// интерфейсном вызове (доминирующий сценарий миграции 8.0.1 -> 8.1.0).
    /// </summary>
    private sealed class DerivedWithExtensionsException(ErrorCategory category)
        : BaseDomainException(category)
    {
        public override IReadOnlyDictionary<string, string> Extensions =>
            new Dictionary<string, string> { ["custom"] = "derived-value" };
    }

    private sealed class MappingConfig : DefaultRfcExceptionHandleConfig
    {
        public override Exception Map(Exception exception) => new ArgumentException("mapped", exception);
    }

    private sealed class TeapotConfig : DefaultRfcExceptionHandleConfig
    {
        public override int ResolveHttpStatusCode(Exception exception) => StatusCodes.Status418ImATeapot;
    }

    // --- фейковый логгер для проверки уровня логирования ---

    private sealed record LogEntry(LogLevel Level, bool HasException);

    private sealed class FakeLogCollector
    {
        private readonly List<LogEntry> _entries = new();

        public IReadOnlyList<LogEntry> Entries
        {
            get
            {
                lock (_entries)
                    return _entries.ToList();
            }
        }

        public void Add(LogEntry entry)
        {
            lock (_entries)
                _entries.Add(entry);
        }
    }

    private sealed class FakeLoggerProvider(FakeLogCollector collector) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new FakeLogger(collector);
        public void Dispose() { }
    }

    private sealed class FakeLogger(FakeLogCollector collector) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
            => collector.Add(new LogEntry(logLevel, exception is not null));
    }
}