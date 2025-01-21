using Dex.Configuration.DataProtection.Purposes;

namespace Dex.Configuration.DataProtection.Tests;

public sealed class DataProtectionPurposeTests
{
    [Theory(DisplayName = "Генерация Purpose успешна для всех имеющихся ApplicationName")]
    [InlineData("AuditWriter")]
    public void PurposeGenerationSucceedForValidApplicationNames(string applicationName)
    {
        Assert.NotNull(new TemplateDataProtectionPurpose().ComputePurpose(applicationName));
    }

    [Theory(DisplayName = "Генерация Purpose неуспешна для несуществующих ApplicationName")]
    [InlineData("NonExistentApp")]
    public void PurposeGenerationFailsForInvalidApplicationNames(string applicationName)
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            new TemplateDataProtectionPurpose().ComputePurpose(applicationName);
        });
    }
}