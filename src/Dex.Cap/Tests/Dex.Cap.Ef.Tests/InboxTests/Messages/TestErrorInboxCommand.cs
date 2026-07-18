using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Ef.Tests.InboxTests.Messages;

/// <summary>
/// Команда, обработчик которой падает всегда: проверяет ретраи и переход в DeadLettered.
/// </summary>
public class TestErrorInboxCommand : IInboxMessage
{
    public static string InboxTypeId => "C5A6B4D3-7E8F-4A0B-9C2D-3E4F5A6B7C8D";

    public string Args { get; init; } = "";
}