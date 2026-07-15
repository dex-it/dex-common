using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Ef.Tests.InboxTests.Messages;

/// <summary>
/// Дискриминатор с кавычкой и фигурной скобкой: первая сломала бы литерал в Postgres,
/// вторая - string.Format внутри EF. Параметризация обязана снять оба вопроса.
/// </summary>
public class HostileDiscriminatorCommand : IInboxMessage
{
    public static string InboxTypeId => "O'Brien-{0}+urn/x#1";

    public string Args { get; init; } = "";
}
