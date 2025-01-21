namespace Dex.Configuration.DataProtection.Tests;

public abstract class DataProtectionTestsBase : IDisposable
{
    protected readonly DirectoryInfo KeysDirectory;

    protected DataProtectionTestsBase()
    {
        KeysDirectory = Directory.CreateDirectory("test_keys_directory");
    }

    public void Dispose()
    {
        KeysDirectory.Delete(true);
    }
}