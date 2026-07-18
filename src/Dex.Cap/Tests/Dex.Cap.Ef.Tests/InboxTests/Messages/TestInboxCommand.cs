using System;
using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Ef.Tests.InboxTests.Messages;

public class TestInboxCommand : IInboxMessage
{
    public static string InboxTypeId => "A3E4F2B1-5C6D-4E8F-9A0B-1C2D3E4F5A6B";

    public string Args { get; init; } = "";

    public Guid TestId { get; init; } = Guid.NewGuid();
}