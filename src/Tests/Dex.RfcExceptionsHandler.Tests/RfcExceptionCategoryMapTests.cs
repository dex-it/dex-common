using Dex.RfcAbstractions;
using NUnit.Framework;

namespace Dex.RfcExceptionsHandler.Tests;

[TestFixture]
public class RfcExceptionCategoryMapTests
{
    [TestCase(ErrorCategory.Validation, 400, RfcErrorCodes.ValidationError)]
    [TestCase(ErrorCategory.BadRequest, 400, RfcErrorCodes.BadRequest)]
    [TestCase(ErrorCategory.UserError, 400, RfcErrorCodes.UserError)]
    [TestCase(ErrorCategory.Unauthorized, 401, RfcErrorCodes.Unauthorized)]
    [TestCase(ErrorCategory.Forbidden, 403, RfcErrorCodes.Forbidden)]
    [TestCase(ErrorCategory.NotFound, 404, RfcErrorCodes.NotFound)]
    [TestCase(ErrorCategory.Conflict, 409, RfcErrorCodes.Conflict)]
    [TestCase(ErrorCategory.AlreadyExists, 409, RfcErrorCodes.AlreadyExist)]
    [TestCase(ErrorCategory.PreconditionFailed, 412, RfcErrorCodes.PreconditionFailed)]
    [TestCase(ErrorCategory.PaymentRequired, 402, RfcErrorCodes.PaymentError)]
    [TestCase(ErrorCategory.TooManyRequests, 429, RfcErrorCodes.TooManyRequests)]
    [TestCase(ErrorCategory.Timeout, 408, RfcErrorCodes.Timeout)]
    [TestCase(ErrorCategory.IntegrationError, 503, RfcErrorCodes.IntegrationError)]
    [TestCase(ErrorCategory.ServiceUnavailable, 503, RfcErrorCodes.ServiceUnavailable)]
    [TestCase(ErrorCategory.Unknown, 500, RfcErrorCodes.InternalServerError)]
    [TestCase((ErrorCategory)999, 500, RfcErrorCodes.InternalServerError)]
    public void Resolve_ReturnsExpectedStatusAndCode(ErrorCategory category, int expectedStatus, string expectedCode)
    {
        var (status, code) = RfcExceptionCategoryMap.Resolve(category);
        Assert.Multiple((Action)(() =>
        {
            Assert.That(status, Is.EqualTo(expectedStatus));
            Assert.That(code, Is.EqualTo(expectedCode));
        }));
    }

    /// <summary>
    /// Страховка полноты: каждое объявленное значение enum (кроме Unknown) должно иметь
    /// собственную ветку в карте — не падать в default (500, internal-server-error).
    /// Иначе новая категория молча деградирует в 500.
    /// </summary>
    [Test]
    public void Resolve_EveryDeclaredCategory_HasDedicatedMapping()
    {
        var fallback = (500, RfcErrorCodes.InternalServerError);

        foreach (var category in Enum.GetValues<ErrorCategory>())
        {
            if (category == ErrorCategory.Unknown)
                continue;

            var result = RfcExceptionCategoryMap.Resolve(category);
            Assert.That(result, Is.Not.EqualTo(fallback),
                $"Категория {category} не имеет собственного маппинга и падает в default (500).");
        }
    }
}