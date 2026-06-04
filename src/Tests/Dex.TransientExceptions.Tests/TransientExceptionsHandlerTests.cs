using System.Net;
using System.Net.Sockets;
using Dex.TransientExceptions.Exceptions;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
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

    private sealed class TestCandidateException(bool isTransient, Exception? inner = null)
        : Exception("test", inner), ITransientExceptionCandidate
    {
        public bool IsTransient { get; } = isTransient;
    }
}