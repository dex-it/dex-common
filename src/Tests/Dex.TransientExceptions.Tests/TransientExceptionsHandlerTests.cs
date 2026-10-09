extern alias PollyV7;

using System.Net;
using System.Net.Sockets;
using Dex.TransientExceptions.Exceptions;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NUnit.Framework;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Refit;

namespace Dex.TransientExceptions.Tests;

[TestFixture]
public class TransientExceptionsHandlerTests
{
    // -------------------------------------------------------------------------
    // Builder / lifecycle
    // -------------------------------------------------------------------------

    [Test]
    public void Check_BeforeBuild_ThrowsInvalidOperationException()
    {
        var handler = new TransientExceptionsHandler();
        Assert.Throws<InvalidOperationException>((Action)(() => handler.Check(new Exception())));
    }

    [Test]
    public void Add_AfterBuild_ThrowsInvalidOperationException()
    {
        var handler = new TransientExceptionsHandler(runBuild: true);
        Assert.Throws<InvalidOperationException>((Action)(() => handler.Add(typeof(IOException))));
    }

    [Test]
    public void Add_NonExceptionType_ThrowsArgumentException()
    {
        var handler = new TransientExceptionsHandler();
        Assert.Throws<ArgumentException>((Action)(() => handler.Add(typeof(string))));
    }

    [Test]
    public void DisableDefaultBehaviour_AfterBuild_ThrowsInvalidOperationException()
    {
        var handler = new TransientExceptionsHandler(runBuild: true);
        Assert.Throws<InvalidOperationException>((Action)(() => handler.DisableDefaultBehaviour()));
    }

    [Test]
    public void Build_CalledTwice_ThrowsInvalidOperationException()
    {
        var handler = new TransientExceptionsHandler();
        handler.Build();
        Assert.Throws<InvalidOperationException>((Action)(() => handler.Build()));
    }

    [Test]
    public void Check_NullException_ThrowsArgumentNullException()
    {
        var handler = new TransientExceptionsHandler(runBuild: true);
        Assert.Throws<ArgumentNullException>((Action)(() => handler.Check(null!)));
    }

    // -------------------------------------------------------------------------
    // Static Default — transient types
    // -------------------------------------------------------------------------

    [Test]
    [TestCase(typeof(TimeoutException))]
    [TestCase(typeof(IOException))]
    [TestCase(typeof(SocketException))]
    [TestCase(typeof(OutOfMemoryException))]
    [TestCase(typeof(OperationCanceledException))]
    public void Default_WellKnownTransientTypes_ReturnsTrue(Type exceptionType)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType)!;
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    public void Default_DbUpdateConcurrencyException_ReturnsTrue()
    {
        var ex = new DbUpdateConcurrencyException();
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    public void Default_PlainException_ReturnsFalse()
    {
        Assert.That(TransientExceptionsHandler.Default.Check(new Exception("boom")), Is.False);
    }

    [Test]
    public void Default_ArgumentException_ReturnsFalse()
    {
        Assert.That(TransientExceptionsHandler.Default.Check(new ArgumentException()), Is.False);
    }

    // -------------------------------------------------------------------------
    // Static Default — HttpRequestException predicates
    // -------------------------------------------------------------------------

    [Test]
    [TestCase(HttpStatusCode.RequestTimeout)]
    [TestCase(HttpStatusCode.TooManyRequests)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.BadGateway)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    public void Default_HttpRequestException_TransientStatusCodes_ReturnsTrue(HttpStatusCode code)
    {
        var ex = new HttpRequestException(null, null, code);
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.Conflict)]
    [TestCase(HttpStatusCode.UnprocessableEntity)]
    public void Default_HttpRequestException_NonTransientStatusCodes_ReturnsFalse(HttpStatusCode code)
    {
        var ex = new HttpRequestException(null, null, code);
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.False);
    }

    // -------------------------------------------------------------------------
    // Static Default — Refit ApiException predicates
    // -------------------------------------------------------------------------

    [Test]
    [TestCase(HttpStatusCode.RequestTimeout)]
    [TestCase(HttpStatusCode.TooManyRequests)]
    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.BadGateway)]
    [TestCase(HttpStatusCode.ServiceUnavailable)]
    public void Default_ApiException_TransientStatusCodes_ReturnsTrue(HttpStatusCode code)
    {
        var ex = ApiException.Create(new HttpRequestMessage(), HttpMethod.Post,
            new HttpResponseMessage(code), new RefitSettings()).Result;
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    [TestCase(HttpStatusCode.BadRequest)]
    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.Conflict)]
    public void Default_ApiException_NonTransientStatusCodes_ReturnsFalse(HttpStatusCode code)
    {
        var ex = ApiException.Create(new HttpRequestMessage(), HttpMethod.Post,
            new HttpResponseMessage(code), new RefitSettings()).Result;
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.False);
    }

    // -------------------------------------------------------------------------
    // Static Default — gRPC RpcException predicates
    // -------------------------------------------------------------------------

    [Test]
    [TestCase(StatusCode.Unknown)]
    [TestCase(StatusCode.Internal)]
    [TestCase(StatusCode.Unavailable)]
    [TestCase(StatusCode.Aborted)]
    [TestCase(StatusCode.DeadlineExceeded)]
    [TestCase(StatusCode.ResourceExhausted)]
    public void Default_RpcException_TransientStatusCodes_ReturnsTrue(StatusCode code)
    {
        var ex = new RpcException(new Status(code, "error"));
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    [TestCase(StatusCode.NotFound)]
    [TestCase(StatusCode.PermissionDenied)]
    [TestCase(StatusCode.InvalidArgument)]
    [TestCase(StatusCode.AlreadyExists)]
    public void Default_RpcException_NonTransientStatusCodes_ReturnsFalse(StatusCode code)
    {
        var ex = new RpcException(new Status(code, "error"));
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.False);
    }

    // -------------------------------------------------------------------------
    // Static Default — предикаты срабатывают на наследниках
    // -------------------------------------------------------------------------

    [Test]
    [TestCase(PostgresErrorCodes.SerializationFailure)]
    [TestCase(PostgresErrorCodes.DeadlockDetected)]
    [TestCase(PostgresErrorCodes.TooManyConnections)]
    public void Default_PostgresException_TransientSqlState_ReturnsTrue(string sqlState)
    {
        Assert.That(TransientExceptionsHandler.Default.Check(Postgres(sqlState)), Is.True);
    }

    [Test]
    [TestCase(PostgresErrorCodes.UndefinedTable)]
    [TestCase(PostgresErrorCodes.UniqueViolation)]
    public void Default_PostgresException_PermanentSqlState_ReturnsFalse(string sqlState)
    {
        Assert.That(TransientExceptionsHandler.Default.Check(Postgres(sqlState)), Is.False);
    }

    [Test]
    public void Default_DbUpdateExceptionWithTransientPostgresInner_ReturnsTrue()
    {
        var ex = new DbUpdateException("save failed", Postgres(PostgresErrorCodes.SerializationFailure));
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    public void Default_DbUpdateExceptionWithPermanentPostgresInner_ReturnsFalse()
    {
        var ex = new DbUpdateException("save failed", Postgres(PostgresErrorCodes.UniqueViolation));
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.False);
    }

    [Test]
    public void Default_DerivedHttpRequestException_TransientStatusCode_ReturnsTrue()
    {
        var ex = new DerivedHttpRequestException(HttpStatusCode.ServiceUnavailable);
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    public void Default_DerivedHttpRequestException_NonTransientStatusCode_ReturnsFalse()
    {
        var ex = new DerivedHttpRequestException(HttpStatusCode.BadRequest);
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.False);
    }

    // -------------------------------------------------------------------------
    // Static Default — отказы Polly
    // -------------------------------------------------------------------------

    [Test]
    [TestCase(typeof(TimeoutRejectedException))]
    [TestCase(typeof(BrokenCircuitException))]
    [TestCase(typeof(IsolatedCircuitException))]
    [TestCase(typeof(OtherPollyRejectionException))]
    public void Default_PollyRejection_ReturnsTrue(Type exceptionType)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType)!;
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    /// <remarks>
    /// Сборка Polly 7 (Polly.dll), как у сервисов на Microsoft.Extensions.Http.Polly: свои типы, не Polly.Core.
    /// </remarks>
    [Test]
    [TestCase(typeof(PollyV7::Polly.Timeout.TimeoutRejectedException))]
    [TestCase(typeof(PollyV7::Polly.CircuitBreaker.BrokenCircuitException))]
    [TestCase(typeof(PollyV7::Polly.Bulkhead.BulkheadRejectedException))]
    public void Default_Polly7Rejection_ReturnsTrue(Type exceptionType)
    {
        var ex = (Exception)Activator.CreateInstance(exceptionType)!;
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    public void Default_PollyRejectionAsInner_ReturnsTrue()
    {
        var ex = new InvalidOperationException("outer", new TimeoutRejectedException());
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    public void Default_RejectionOutsidePollyNamespace_ReturnsFalse()
    {
        Assert.That(TransientExceptionsHandler.Default.Check(new NotPolly.ExecutionRejectedException()), Is.False);
    }

    [Test]
    public void CustomHandler_DisableDefaultBehaviour_PollyRejection_ReturnsFalse()
    {
        var handler = new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .Build();

        Assert.That(handler.Check(new TimeoutRejectedException()), Is.False);
    }

    // -------------------------------------------------------------------------
    // ITransientException marker interface
    // -------------------------------------------------------------------------

    [Test]
    public void Default_TransientException_ReturnsTrue()
    {
        Assert.That(TransientExceptionsHandler.Default.Check(new TransientException()), Is.True);
    }

    [Test]
    public void Default_TransientExceptionAsInner_ReturnsTrue()
    {
        var outer = new Exception("outer", new TransientException("inner"));
        Assert.That(TransientExceptionsHandler.Default.Check(outer), Is.True);
    }

    // -------------------------------------------------------------------------
    // ITransientExceptionCandidate — финальное решение, не идти дальше по стеку
    // -------------------------------------------------------------------------

    [Test]
    public void Default_CandidateIsTransientTrue_ReturnsTrue()
    {
        var ex = new TestCandidateException(isTransient: true);
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    public void Default_CandidateIsTransientFalse_ReturnsFalse()
    {
        var ex = new TestCandidateException(isTransient: false);
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.False);
    }

    [Test]
    public void Default_CandidateIsTransientFalse_WithTransientInner_ReturnsFalse()
    {
        // Ключевой кейс: кандидат говорит false, inner — ApiException 5xx.
        // Должен победить кандидат, не StaticCheck по inner.
        var inner = ApiException.Create(new HttpRequestMessage(), HttpMethod.Post,
            new HttpResponseMessage(HttpStatusCode.InternalServerError), new RefitSettings()).Result;
        var outer = new TestCandidateException(isTransient: false, inner);
        Assert.That(TransientExceptionsHandler.Default.Check(outer), Is.False);
    }

    [Test]
    public void Default_CandidateIsTransientTrue_WithNonTransientInner_ReturnsTrue()
    {
        var inner = new ArgumentException("not transient");
        var outer = new TestCandidateException(isTransient: true, inner);
        Assert.That(TransientExceptionsHandler.Default.Check(outer), Is.True);
    }

    [Test]
    public void Default_CandidateAsInner_IsTransientFalse_ReturnsFalse()
    {
        // Кандидат в inner exception — его решение тоже финальное
        var inner = new TestCandidateException(isTransient: false);
        var outer = new Exception("outer", inner);
        Assert.That(TransientExceptionsHandler.Default.Check(outer), Is.False);
    }

    [Test]
    public void Default_CandidateAsInner_IsTransientTrue_ReturnsTrue()
    {
        var inner = new TestCandidateException(isTransient: true);
        var outer = new Exception("outer", inner);
        Assert.That(TransientExceptionsHandler.Default.Check(outer), Is.True);
    }

    // -------------------------------------------------------------------------
    // RetryAll
    // -------------------------------------------------------------------------

    [Test]
    public void RetryAll_AnyException_ReturnsTrue()
    {
        Assert.That(TransientExceptionsHandler.RetryAll.Check(new Exception()), Is.True);
        Assert.That(TransientExceptionsHandler.RetryAll.Check(new ArgumentException()), Is.True);
        Assert.That(TransientExceptionsHandler.RetryAll.Check(new InvalidOperationException()), Is.True);
    }

    // -------------------------------------------------------------------------
    // Builder — custom types и predicates
    // -------------------------------------------------------------------------

    [Test]
    public void CustomHandler_AddedType_ReturnsTrue()
    {
        var handler = new TransientExceptionsHandler()
            .Add(typeof(InvalidOperationException))
            .Build();

        Assert.That(handler.Check(new InvalidOperationException()), Is.True);
    }

    [Test]
    public void CustomHandler_AddedType_OtherException_ReturnsFalse()
    {
        var handler = new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .Add(typeof(InvalidOperationException))
            .Build();

        Assert.That(handler.Check(new ArgumentException()), Is.False);
    }

    [Test]
    public void CustomHandler_Predicate_MatchingCondition_ReturnsTrue()
    {
        var handler = new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .Add<ArgumentException>(ex => ex.Message.Contains("retry"))
            .Build();

        Assert.That(handler.Check(new ArgumentException("please retry")), Is.True);
    }

    [Test]
    public void CustomHandler_Predicate_NonMatchingCondition_ReturnsFalse()
    {
        var handler = new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .Add<ArgumentException>(ex => ex.Message.Contains("retry"))
            .Build();

        Assert.That(handler.Check(new ArgumentException("permanent error")), Is.False);
    }

    [Test]
    public void CustomHandler_PredicateOnBaseType_AppliesToDerived()
    {
        var handler = new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .Add<IOException>(ex => ex.Message.Contains("retry"))
            .Build();

        Assert.That(handler.Check(new FileNotFoundException("please retry")), Is.True);
        Assert.That(handler.Check(new FileNotFoundException("permanent error")), Is.False);
    }

    [Test]
    public void CustomHandler_PredicatesOnBaseAndDerived_AnyMatchReturnsTrue()
    {
        var handler = new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .Add<ArgumentNullException>(_ => false)
            .Add<ArgumentException>(_ => true)
            .Build();

        Assert.That(handler.Check(new ArgumentNullException()), Is.True);
    }

    [Test]
    public void CustomHandler_DisableDefaultBehaviour_WellKnownTransientType_ReturnsFalse()
    {
        var handler = new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .Build();

        Assert.That(handler.Check(new TimeoutException()), Is.False);
    }

    [Test]
    public void CustomHandler_AddedTypeWithInheritance_SubclassReturnsTrue()
    {
        var handler = new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .Add(typeof(IOException))
            .Build();

        // FileNotFoundException наследует IOException
        Assert.That(handler.Check(new FileNotFoundException()), Is.True);
    }

    // -------------------------------------------------------------------------
    // Inner exceptions traversal
    // -------------------------------------------------------------------------

    [Test]
    public void Default_TransientInnerAtDepth2_ReturnsTrue()
    {
        var ex = new Exception("L1", new Exception("L2", new TimeoutException()));
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    public void Constructor_InnerExceptionsSearchDepth0_DoesNotCheckInner()
    {
        var handler = new TransientExceptionsHandler([typeof(TimeoutException)], innerExceptionsSearchDepth: 0, runBuild: true, disableDefaultBehaviour: true);

        Assert.That(handler.Check(new Exception("outer", new TimeoutException())), Is.False);
    }

    [Test]
    public void Constructor_NegativeInnerExceptionsSearchDepth_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => _ = new TransientExceptionsHandler(innerExceptionsSearchDepth: -1)));
    }

    [Test]
    public void SetInnerExceptionsSearchDepth_Negative_ThrowsArgumentOutOfRangeException()
    {
        var handler = new TransientExceptionsHandler();
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() => handler.SetInnerExceptionsSearchDepth(-1)));
    }

    [Test]
    public void CustomHandler_InnerExceptionSearchDepth_CountsOuterExceptionAsFirstLevel()
    {
        var handler = new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .Add(typeof(TimeoutException))
            .SetInnerExceptionsSearchDepth(2)
            .Build();

        Assert.That(handler.Check(new Exception("L1", new TimeoutException())), Is.True);
        Assert.That(handler.Check(new Exception("L1", new Exception("L2", new TimeoutException()))), Is.False);
    }

    [Test]
    public void Default_AggregateWithTransientNotFirst_ReturnsTrue()
    {
        var ex = new AggregateException(new ArgumentException(), new TimeoutException());
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    public void Default_AggregateWithTransientPredicateNotFirst_ReturnsTrue()
    {
        var ex = new AggregateException(new ArgumentException(), Postgres(PostgresErrorCodes.DeadlockDetected));
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    public void Default_AggregateWithTransientMarkerNotFirst_ReturnsTrue()
    {
        var ex = new AggregateException(new ArgumentException(), new TransientException());
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    public void Default_AggregateNestedInChain_ReturnsTrue()
    {
        var ex = new InvalidOperationException("outer", new AggregateException(new ArgumentException(), new TimeoutException()));
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }

    [Test]
    public void Default_AggregateWithoutTransient_ReturnsFalse()
    {
        var ex = new AggregateException(new ArgumentException(), new InvalidOperationException());
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.False);
    }

    [Test]
    public void CustomHandler_AggregateBeyondSearchDepth_ReturnsFalse()
    {
        var handler = new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .Add(typeof(TimeoutException))
            .SetInnerExceptionsSearchDepth(2)
            .Build();

        var ex = new Exception("L1", new AggregateException(new ArgumentException(), new TimeoutException()));
        Assert.That(handler.Check(ex), Is.False);
    }

    // -------------------------------------------------------------------------
    // Маркеры в ветках AggregateException — решение своей ветки, не соседней
    // -------------------------------------------------------------------------

    [Test]
    public void Default_AggregateTransientBranchAndCandidateFalse_ReturnsTrueInAnyOrder()
    {
        var timeout = new TimeoutException();
        var candidate = new TestCandidateException(isTransient: false);

        Assert.That(TransientExceptionsHandler.Default.Check(new AggregateException(timeout, candidate)), Is.True);
        Assert.That(TransientExceptionsHandler.Default.Check(new AggregateException(candidate, timeout)), Is.True);
    }

    [Test]
    public void Default_AggregateMarkerAndCandidateFalse_ReturnsTrueInAnyOrder()
    {
        var marker = new TransientException();
        var candidate = new TestCandidateException(isTransient: false);

        Assert.That(TransientExceptionsHandler.Default.Check(new AggregateException(marker, candidate)), Is.True);
        Assert.That(TransientExceptionsHandler.Default.Check(new AggregateException(candidate, marker)), Is.True);
    }

    [Test]
    public void Default_AggregateCandidateFalseAndNonTransient_ReturnsFalse()
    {
        // таймаут под кандидатом остаётся под его вето и в ветке агрегата
        var candidate = new TestCandidateException(isTransient: false, new TimeoutException());
        var ex = new AggregateException(candidate, new ArgumentException());

        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.False);
    }

    [Test]
    public void Default_AggregateWithCandidateFalseUnderTransientOuter_ReturnsFalse()
    {
        var ex = new TimeoutException("outer", new AggregateException(new TestCandidateException(isTransient: false), new ArgumentException()));
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.False);
    }

    [Test]
    public void CustomHandler_AggregateBranchWithCandidate_RespectsSearchDepth()
    {
        TransientExceptionsHandler Handler(int depth) => new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .Add(typeof(TimeoutException))
            .SetInnerExceptionsSearchDepth(depth)
            .Build();

        // таймаут на втором уровне вложенности: виден при глубине 3, не виден при 2
        var ex = new AggregateException(new TestCandidateException(isTransient: false), new Exception("L1", new TimeoutException()));

        Assert.That(Handler(3).Check(ex), Is.True);
        Assert.That(Handler(2).Check(ex), Is.False);
    }

    [Test]
    public void CustomHandler_MarkerBeyondSearchDepth_IsIgnored()
    {
        TransientExceptionsHandler Handler(int depth) => new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .SetInnerExceptionsSearchDepth(depth)
            .Build();

        var candidate = new TestCandidateException(isTransient: true);

        Assert.That(Handler(1).Check(new Exception("L1", candidate)), Is.False);
        Assert.That(Handler(2).Check(new Exception("L1", new AggregateException(candidate))), Is.False);
        Assert.That(Handler(3).Check(new Exception("L1", new AggregateException(candidate))), Is.True);
    }

    [Test]
    public void AggregateWithoutMarkers_TypesAboveAndOfAggregateDecide()
    {
        var timeoutOverAggregate = new TimeoutException("outer", new AggregateException(new ArgumentException()));
        Assert.That(TransientExceptionsHandler.Default.Check(timeoutOverAggregate), Is.True);

        var handler = new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .Add(typeof(AggregateException))
            .Build();
        Assert.That(handler.Check(new AggregateException(new ArgumentException())), Is.True);
    }

    [Test]
    public void CustomHandler_InnerExceptionSearchDepth0_DoesNotCheckInner()
    {
        var handler = new TransientExceptionsHandler()
            .DisableDefaultBehaviour()
            .Add(typeof(TimeoutException))
            .SetInnerExceptionsSearchDepth(0)
            .Build();

        var ex = new Exception("outer", new TimeoutException());
        Assert.That(handler.Check(ex), Is.False);
    }

    // -------------------------------------------------------------------------
    // Implicit conversion to Func<Exception, bool>
    // -------------------------------------------------------------------------

    [Test]
    public void ImplicitConversion_ToFunc_Works()
    {
        Func<Exception, bool> func = TransientExceptionsHandler.Default;
        Assert.That(func(new TimeoutException()), Is.True);
        Assert.That(func(new ArgumentException()), Is.False);
    }

    [Test]
    public void ToFunc_Works()
    {
        var func = TransientExceptionsHandler.Default.ToFunc();
        Assert.That(func(new TimeoutException()), Is.True);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static PostgresException Postgres(string sqlState) => new("error", "ERROR", "ERROR", sqlState);

    private sealed class DerivedHttpRequestException(HttpStatusCode statusCode) : HttpRequestException(null, null, statusCode);

    // любой отказ Polly, в том числе из пакетов вне Polly.Core (RateLimiterRejectedException, BulkheadRejectedException)
    private sealed class OtherPollyRejectionException : ExecutionRejectedException;

    // тот же короткий тип вне пространства имён Polly
    private static class NotPolly
    {
        public sealed class ExecutionRejectedException : Exception;
    }

    private sealed class TestCandidateException(bool isTransient, Exception? inner = null)
        : Exception("test", inner), ITransientExceptionCandidate
    {
        public bool IsTransient { get; } = isTransient;
    }

    // Симулирует класс который одновременно ITransientException (базовый) и ITransientExceptionCandidate (переопределяет)
    private sealed class TestCandidateOverridesTransientException(bool isTransient)
        : Exception("test"), ITransientException, ITransientExceptionCandidate
    {
        public bool IsTransient { get; } = isTransient;
    }

    // -------------------------------------------------------------------------
    // ITransientExceptionCandidate имеет приоритет над ITransientException
    // -------------------------------------------------------------------------

    [Test]
    public void Check_CandidateFalse_WhenAlsoImplementsITransientException_ReturnsFalse()
    {
        // Класс реализует оба интерфейса — Candidate(false) должен побеждать ITransientException
        var ex = new TestCandidateOverridesTransientException(isTransient: false);
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.False);
    }

    [Test]
    public void Check_CandidateTrue_WhenAlsoImplementsITransientException_ReturnsTrue()
    {
        var ex = new TestCandidateOverridesTransientException(isTransient: true);
        Assert.That(TransientExceptionsHandler.Default.Check(ex), Is.True);
    }
}