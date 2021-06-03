namespace Dex.Cap.Outbox
{
    public interface IOutboxSerializer
    {
        string Serialize<T>(T message);
        T Deserialize<T>(string message);
        object Deserialize(string message);
    }
}