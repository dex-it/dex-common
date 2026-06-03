using System.ComponentModel.DataAnnotations;
using Grpc.Core;
using NUnit.Framework;

namespace Dex.RfcExceptionsHandler.Tests;

[TestFixture]
public class DefaultRfcExceptionHandleConfigTests
{
    private DefaultRfcExceptionHandleConfig _sut = null!;

    [SetUp]
    public void SetUp() => _sut = new DefaultRfcExceptionHandleConfig();

    [Test]
    public void Map_ReturnsIdenticalInstance()
    {
        var ex = new Exception("test");
        Assert.That(_sut.Map(ex), Is.SameAs(ex));
    }

    [Test]
    public void JsonSerializerOptions_IsNotNull()
    {
        Assert.That(_sut.JsonSerializerOptions, Is.Not.Null);
    }

    [Test]
    public void ResolveHttpStatusCode_UnauthorizedAccessException_Returns403()
        => Assert.That(_sut.ResolveHttpStatusCode(new UnauthorizedAccessException()), Is.EqualTo(StatusCodes.Status403Forbidden));

    [Test]
    public void ResolveHttpStatusCode_ArgumentException_Returns400()
        => Assert.That(_sut.ResolveHttpStatusCode(new ArgumentException()), Is.EqualTo(StatusCodes.Status400BadRequest));

    [Test]
    public void ResolveHttpStatusCode_ArgumentNullException_Returns400()
        => Assert.That(_sut.ResolveHttpStatusCode(new ArgumentNullException($"x")), Is.EqualTo(StatusCodes.Status400BadRequest));

    [Test]
    public void ResolveHttpStatusCode_ValidationException_Returns400()
        => Assert.That(_sut.ResolveHttpStatusCode(new ValidationException()), Is.EqualTo(StatusCodes.Status400BadRequest));

    [Test]
    public void ResolveHttpStatusCode_TimeoutException_Returns408()
        => Assert.That(_sut.ResolveHttpStatusCode(new TimeoutException()), Is.EqualTo(StatusCodes.Status408RequestTimeout));

    [Test]
    public void ResolveHttpStatusCode_OperationCanceledException_Returns499()
        => Assert.That(_sut.ResolveHttpStatusCode(new OperationCanceledException()), Is.EqualTo(StatusCodes.Status499ClientClosedRequest));

    [Test]
    public void ResolveHttpStatusCode_TaskCanceledException_Returns499()
        => Assert.That(_sut.ResolveHttpStatusCode(new TaskCanceledException()), Is.EqualTo(StatusCodes.Status499ClientClosedRequest));

    [Test]
    public void ResolveHttpStatusCode_GenericException_Returns500()
        => Assert.That(_sut.ResolveHttpStatusCode(new Exception()), Is.EqualTo(StatusCodes.Status500InternalServerError));

    [TestCase(StatusCode.PermissionDenied, StatusCodes.Status403Forbidden)]
    [TestCase(StatusCode.Unauthenticated, StatusCodes.Status401Unauthorized)]
    [TestCase(StatusCode.InvalidArgument, StatusCodes.Status400BadRequest)]
    [TestCase(StatusCode.AlreadyExists, StatusCodes.Status409Conflict)]
    [TestCase(StatusCode.Unavailable, StatusCodes.Status408RequestTimeout)]
    [TestCase(StatusCode.NotFound, StatusCodes.Status404NotFound)]
    [TestCase(StatusCode.Cancelled, StatusCodes.Status499ClientClosedRequest)]
    [TestCase(StatusCode.Internal, StatusCodes.Status500InternalServerError)]
    [TestCase(StatusCode.Unknown, StatusCodes.Status500InternalServerError)]
    [TestCase(StatusCode.DataLoss, StatusCodes.Status500InternalServerError)]
    public void ResolveHttpStatusCode_RpcException_ReturnsExpectedStatus(StatusCode rpcStatus, int expectedHttp)
    {
        var ex = new RpcException(new Status(rpcStatus, string.Empty));
        Assert.That(_sut.ResolveHttpStatusCode(ex), Is.EqualTo(expectedHttp));
    }
}