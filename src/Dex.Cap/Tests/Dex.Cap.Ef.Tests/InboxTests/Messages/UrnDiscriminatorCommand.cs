using Dex.Cap.Inbox.Interfaces;

namespace Dex.Cap.Ef.Tests.InboxTests.Messages;

/// <summary>
/// Дискриминатор в формате MessageUrn MassTransit: двоеточия обязаны доходить до SQL захвата.
/// </summary>
public class UrnDiscriminatorCommand : IInboxMessage
{
    public static string InboxTypeId => "urn:message:Dex.Cap.Ef.Tests.InboxTests.Messages:UrnDiscriminatorCommand";

    public string Args { get; init; } = "";
}