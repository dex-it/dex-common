using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Dex.Configuration.DataProtection.DataEncryption;
using Microsoft.Extensions.Configuration;

namespace Dex.Configuration.DataProtection.Cli;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Option<FileInfo> file = new(
            name: "--file",
            description: "File path");

        Option<string[]> configurationKeys = new(
            name: "--configuration-key",
            description: "Configuration key");

        Option<DirectoryInfo> keysDirectory = new(
            name: "--keys-directory",
            description: "Keys directory"
        );

        Option<string> projectName = new(
            name: "--project-name",
            description: "Project name"
        );

        Option<string> applicationName = new(
            name: "--application-name",
            description: "Application name"
        );

        Option<string> data = new(
            name: "--data",
            description: "Data to protect/unprotect"
        );

        RootCommand rootCommand = new("Dex Data protection CLI");
        Command protectCommand = new("protect", "Protect data")
        {
            keysDirectory,
            projectName,
            applicationName,
            data
        };
        protectCommand.SetHandler(ProtectCommandHandler, keysDirectory, applicationName, projectName, data);
        rootCommand.AddCommand(protectCommand);

        Command protectEncryptedCommand = new("protect-encrypted", "Protect Internal Data")
        {
            keysDirectory,
            projectName,
            applicationName,
            data
        };
        protectEncryptedCommand.SetHandler(ProtectEncryptedCommandHandler, keysDirectory, applicationName, projectName,
            data);
        rootCommand.AddCommand(protectEncryptedCommand);

#if ADD_UNPROTECT

        Command unprotectCommand = new("unprotect", "Unprotect data")
        {
            keysDirectory,
            projectName,
            applicationName,
            data
        };
        unprotectCommand.SetHandler(UnprotectCommandHandler, keysDirectory, applicationName, projectName, data);
        rootCommand.AddCommand(unprotectCommand);

        Command unprotectFile = new("unprotect-file", "Unprotect file")
        {
            keysDirectory,
            projectName,
            file,
            configurationKeys
        };
        unprotectFile.SetHandler(UnprotectFileCommandHandler, keysDirectory, projectName, file, configurationKeys);
        rootCommand.AddCommand(unprotectFile);

#endif

#if DEBUG

        Command encryptCommand = new("encrypt", "Encrypt Data")
        {
            data
        };
        encryptCommand.SetHandler(EncryptCommandHandler, data);
        rootCommand.AddCommand(encryptCommand);

#endif

        return await rootCommand.InvokeAsync(args);
    }

    private static void ProtectCommandHandler(
        DirectoryInfo keysDirectory,
        string applicationName,
        string projectName,
        string plaintext)
    {
        var protectedData = DataProtection.ProtectData(keysDirectory, applicationName, projectName, plaintext);
        Console.WriteLine(protectedData);
    }

    private static void ProtectEncryptedCommandHandler(
        DirectoryInfo keysDirectory,
        string applicationName,
        string projectName,
        string encryptedData)
    {
        var protectedData =
            DataProtection.ProtectEncryptedData(keysDirectory, applicationName, projectName, encryptedData);

        Console.WriteLine(protectedData);
    }

#if ADD_UNPROTECT

    private static void UnprotectCommandHandler(
        DirectoryInfo keysDirectory,
        string applicationName,
        string projectName,
        string protectedData)
    {
        var plaintext = DataProtection.UnprotectData(keysDirectory, applicationName, projectName, protectedData);
        Console.WriteLine(plaintext);
    }

    private static void UnprotectFileCommandHandler(
        DirectoryInfo keysDirectory,
        string projectName,
        FileInfo file,
        string[] configurationKeys)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(file.FullName).Build();

        var applicationName = configuration["ConfigurationProtectionOptions:ApplicationName"];
        foreach (var configurationKey in configurationKeys)
        {
            var protectedData = configuration[configurationKey];
            var plaintext = DataProtection.UnprotectData(keysDirectory, applicationName, projectName, protectedData);

            Console.WriteLine(configurationKey);
            Console.WriteLine(plaintext);
        }
    }

#endif

#if DEBUG
    private static void EncryptCommandHandler(
        string plainText)
    {
        var encryptedData = DataEncryptor.Encrypt(plainText);

        Console.WriteLine(encryptedData);
    }
#endif
}