using Dex.RfcExceptions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Dex.RfcExceptionsHandler.Tests;

[TestFixture]
public class RfcExceptionCategoryMapTests
{
    [TestCase(ErrorCategory.Validation, 400, "/problems/validation-error")]
    [TestCase(ErrorCategory.BadRequest, 400, "/problems/bad-request")]
    [TestCase(ErrorCategory.Unauthorized, 401, "/problems/unauthorized")]
    [TestCase(ErrorCategory.Forbidden, 403, "/problems/forbidden")]
    [TestCase(ErrorCategory.NotFound, 404, "/problems/not-found")]
    [TestCase(ErrorCategory.Conflict, 409, "/problems/conflict")]
    [TestCase(ErrorCategory.AlreadyExists, 409, "/problems/conflict/already-exist")]
    [TestCase(ErrorCategory.PreconditionFailed, 412, "/problems/precondition-failed")]
    [TestCase(ErrorCategory.PaymentRequired, 402, "/problems/payment-error")]
    [TestCase(ErrorCategory.TooManyRequests, 429, "/problems/too-many-requests")]
    [TestCase(ErrorCategory.Timeout, 408, "/problems/request-timeout")]
    [TestCase(ErrorCategory.IntegrationError, 412, "/problems/integration-error")]
    [TestCase(ErrorCategory.ServiceUnavailable, 503, "/problems/service-unavailable")]
    [TestCase(ErrorCategory.Unknown, 500, "/problems/internal-server-error")]
    public void Resolve_ReturnsExpectedStatusAndType(ErrorCategory category, int expectedStatus, string expectedType)
    {
        var (status, type) = RfcExceptionCategoryMap.Resolve(category);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(status, Is.EqualTo(expectedStatus));
            Assert.That(type, Is.EqualTo(expectedType));
        }));
    }
}