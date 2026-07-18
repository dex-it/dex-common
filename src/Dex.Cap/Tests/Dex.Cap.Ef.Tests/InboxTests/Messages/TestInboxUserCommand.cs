using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Ef.Tests.InboxTests.Messages;

/// <summary>
/// Команда с бизнес-эффектом: обработчик создаёт пользователя через тот же DbContext.
/// Нужна, чтобы проверить атомарность эффекта и статуса.
/// </summary>
public class TestInboxUserCommand : IInboxMessage
{
    public static string InboxTypeId => "B4F5A3C2-6D7E-4F9A-8B1C-2D3E4F5A6B7C";

    public string UserName { get; init; } = "";

    /// <summary>
    /// Заставить обработчик упасть ПОСЛЕ создания пользователя, чтобы проверить откат.
    /// </summary>
    public bool ThrowAfterEffect { get; init; }
}