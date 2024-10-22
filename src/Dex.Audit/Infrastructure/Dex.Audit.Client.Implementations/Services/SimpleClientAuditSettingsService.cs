using System.Text.Json;
using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dex.Audit.Client.Grpc.Services;

/// <summary>
/// Simple implementation of <see cref="IAuditSettingsService"/>
/// </summary>
/// <param name="settingsCacheRepository"><see cref="IAuditSettingsCacheRepository"/></param>
/// <param name="configuration"><see cref="IConfiguration"/></param>
/// <param name="logger"><see cref="ILogger{TCategoryName}"/></param>
public class SimpleClientAuditSettingsService(
    IAuditSettingsCacheRepository settingsCacheRepository,
    IConfiguration configuration,
    ILogger<SimpleClientAuditSettingsService> logger) : IAuditSettingsService
{
    public async Task<AuditSettings?> GetOrGetAndUpdateSettingsAsync(string eventType, CancellationToken cancellationToken = default)
    {
        var setting = await settingsCacheRepository.GetAsync(eventType, cancellationToken);

        if (setting != null)
        {
            return setting;
        }

        var settings = await GetSettingsFromServerAsync(cancellationToken);

        if (settings == null || settings.Length == 0)
        {
            return null;
        }

        await settingsCacheRepository
            .AddRangeAsync(settings, cancellationToken)
            .ConfigureAwait(false);

        return settings.FirstOrDefault(auditSettings => auditSettings.EventType == eventType);
    }

    protected virtual async Task<AuditSettings[]?> GetSettingsFromServerAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using var httpClient = new HttpClient(handler);
            using var result =
                await httpClient.GetAsync(
                    configuration.GetConnectionString("AuditServerSettingsAddress"),
                    cancellationToken);

            await using var stream = await result.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<AuditSettings[]?>(
                stream,
                cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, exception.Message);
            return [];
        }
    }
}