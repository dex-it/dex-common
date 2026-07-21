using Dex.RfcAbstractions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;

namespace Dex.RfcExceptionsHandler.Tests;

[TestFixture]
public class RfcExceptionCategoryMapTests
{
    [TestCase(ErrorCategory.Validation, 400, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.ValidationError)]
    [TestCase(ErrorCategory.BadRequest, 400, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.BadRequest)]
    [TestCase(ErrorCategory.UserError, 400, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.UserError)]
    [TestCase(ErrorCategory.Unauthorized, 401, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.Unauthorized)]
    [TestCase(ErrorCategory.Forbidden, 403, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.Forbidden)]
    [TestCase(ErrorCategory.NotFound, 404, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.NotFound)]
    [TestCase(ErrorCategory.Conflict, 409, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.Conflict)]
    [TestCase(ErrorCategory.AlreadyExists, 409, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.AlreadyExist)]
    [TestCase(ErrorCategory.PreconditionFailed, 412, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.PreconditionFailed)]
    [TestCase(ErrorCategory.PaymentRequired, 402, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.PaymentError)]
    [TestCase(ErrorCategory.TooManyRequests, 429, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.TooManyRequests)]
    [TestCase(ErrorCategory.Timeout, 408, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.RequestTimeout)]
    [TestCase(ErrorCategory.IntegrationError, 412, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.IntegrationError)]
    [TestCase(ErrorCategory.ServiceUnavailable, 503, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.ServiceUnavailable)]
    [TestCase(ErrorCategory.Unknown, 500, RfcErrorCodes.ProblemTypePrefix + RfcErrorCodes.InternalServerError)]
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