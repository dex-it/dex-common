using System;
using System.Threading;
using Dex.Cap.Inbox.Models;

namespace Dex.Cap.Inbox.Jobs;

/// <summary>
/// Задача инбокса с захваченной арендой.
/// </summary>
/// <remarks>
/// Internal, а не public: тип фигурирует только во внутренних контрактах, реализация имеет internal
/// конструктор, и снаружи его нельзя ни получить, ни осмысленно реализовать. У Outbox аналог публичен,
/// но это лишняя публичная поверхность, а не точка расширения.
/// </remarks>
internal interface IInboxLockedJob : IDisposable
{
    /// <summary>
    /// Ключ идемпотентности захваченной аренды.
    /// </summary>
    Guid LockId { get; }

    InboxEnvelope Envelope { get; }

    /// <summary>
    /// Отражает время жизни захваченной аренды.
    /// </summary>
    CancellationToken LockToken { get; }
}