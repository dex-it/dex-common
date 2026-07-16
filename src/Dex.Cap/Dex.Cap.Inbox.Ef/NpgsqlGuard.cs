using System;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Dex.Cap.Inbox.Ef;

/// <summary>
/// Проверка того, что за DbContext стоит PostgreSQL.
/// </summary>
/// <remarks>
/// Провайдеры данных инбокса опираются на SQL, специфичный для PostgreSQL (FOR UPDATE SKIP LOCKED,
/// ON CONFLICT, ctid), поэтому чужой провайдер обязан отвергаться явно, а не падать позже синтаксической
/// ошибкой из глубины драйвера.
/// </remarks>
internal static class NpgsqlGuard
{
    private const string NpgsqlProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";

    /// <exception cref="NotSupportedException">За DbContext стоит не PostgreSQL.</exception>
    public static void EnsureNpgsql(this DatabaseFacade database)
    {
        var providerName = database.ProviderName;

        if (providerName is not NpgsqlProviderName)
        {
            throw new NotSupportedException($"The provider {providerName} is not supported.");
        }
    }
}