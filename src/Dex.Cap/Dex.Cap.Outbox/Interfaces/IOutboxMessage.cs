using System.Diagnostics.CodeAnalysis;

namespace Dex.Cap.Outbox.Interfaces
{
    [SuppressMessage("Design", "CA1040:Не используйте пустые интерфейсы", Justification = "Используется как флаг")]
    public interface IOutboxMessage
    {
    }
}