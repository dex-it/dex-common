using Dex.Configuration.DataProtection.DataEncryption;

namespace Dex.Configuration.DataProtection.Tests;

public sealed class DataProtectionTests : DataProtectionTestsBase
{
    [Theory(DisplayName = "При передачи данных на шифрование они шифруются и не совпадают с изначальными значениями")]
    [InlineData("AuditWriter", "Template", "testdatatoprotect")]
    public void PlainDataDataProtection_Protect_NotSameStringAndKeysCreated(string applicationName, string projectName,
        string plainData)
    {
        var protectedData = DataProtection.ProtectData(KeysDirectory, applicationName, projectName, plainData);

        Assert.NotEqual(plainData, protectedData);
        Assert.True(KeysDirectory.EnumerateFiles().Any());
    }

    [Theory(DisplayName = "При вызове методов шифрования и расшифровки получаются изначальные данные")]
    [InlineData("AuditWriter", "Template", "datatoprotect")]
    public void PlainDataDataProtection_ProtectAndUnprotect_SameStringAndKeysCreated(string applicationName,
        string projectName, string plainData)
    {
        var protectedData = DataProtection.ProtectData(KeysDirectory, applicationName, projectName, plainData);
        var unprotectedData =
            DataProtection.UnprotectData(KeysDirectory, applicationName, projectName, protectedData);

        Assert.Equal(plainData, unprotectedData);
        Assert.True(KeysDirectory.EnumerateFiles().Any());
    }

    [Theory(DisplayName =
        "При вызове методов шифрования вшитым в сборку ключом, затем расшифровывания им же и вновь шифрования внешним ключом изначальные данные")]
    [InlineData("AuditWriter", "Template", "datatoprotect")]
    public void PlainDataDataEncryption_DecryptAndProtect_SameStringCreated(
        string applicationName,
        string projectName,
        string plainData)
    {
        var encryptedData = DataEncryptor.Encrypt(plainData);

        var protectedData =
            DataProtection.ProtectEncryptedData(KeysDirectory, applicationName, projectName, encryptedData);

        var unprotectedData =
            DataProtection.UnprotectData(KeysDirectory, applicationName, projectName, protectedData);

        Assert.Equal(plainData, unprotectedData);
        Assert.True(KeysDirectory.EnumerateFiles().Any());
    }
}