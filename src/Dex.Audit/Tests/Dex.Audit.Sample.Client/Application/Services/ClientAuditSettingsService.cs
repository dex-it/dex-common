﻿using Dex.Audit.Client.Abstractions.Interfaces;
using Dex.Audit.Domain.Entities;
using Newtonsoft.Json;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Dex.Audit.ClientSample.Application.Services;

public class ClientAuditSettingsService(
    IAuditCacheRepository cacheRepository,
    IConfiguration configuration,
    ILogger<ClientAuditSettingsService> logger) : IAuditSettingsService
{
    public async Task<AuditSettings?> GetOrGetAndUpdateSettingsAsync(string eventType, CancellationToken cancellationToken = default)
    {
        var setting = await cacheRepository.GetAsync(eventType, cancellationToken);

        if (setting != null)
        {
            return setting;
        }

        var settings = await GetSettingsFromServerAsync(cancellationToken);

        if (settings == null)
        {
            return null;
        }

        await cacheRepository
            .AddRangeAsync(settings, cancellationToken)
            .ConfigureAwait(false);

        return await cacheRepository.GetAsync(eventType, cancellationToken);
    }

    private async Task<IEnumerable<AuditSettings>?> GetSettingsFromServerAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            using var httpClient = new HttpClient(handler);
            using var result =
                await httpClient.GetAsync(
                    configuration.GetConnectionString("ServerSettingsAddress"),
                    cancellationToken);

            var serializer = new JsonSerializer();
            using var sr = new StreamReader(await result.Content.ReadAsStreamAsync(cancellationToken));
            using var jsonTextReader = new JsonTextReader(sr);
            return serializer.Deserialize<IEnumerable<AuditSettings>?>(jsonTextReader);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, exception.Message);
            return null;
        }
    }
}