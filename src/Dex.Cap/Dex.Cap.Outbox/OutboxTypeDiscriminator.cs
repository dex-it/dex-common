namespace Dex.Cap.Outbox;

internal class OutboxTypeDiscriminator<TKey> where TKey : notnull
{
    private BiDictionary<TKey, string> Discriminator { get; } = new();

    public void Add(TKey key, string value)
    {
        Discriminator.Add(key, value);
    }

    public bool GetKey(string value, out TKey key)
    {
        return Discriminator.TryGetKey(value, out key);
    }

    public bool GetValue(TKey key, out string value)
    {
        return Discriminator.TryGetValue(key, out value);
    }
}