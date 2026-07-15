using System;

namespace Dex.Cap.Inbox.Interfaces;

public interface IInboxSerializer
{
    string Serialize(Type type, object obj);
    object? Deserialize(Type type, string input);
}
