using System.Text.Json;

namespace Dex.ResponseSigning.Serialization;

internal class SigningDataSerializationOptions
{
    internal JsonSerializerOptions SerializerOptions { get; }

    public SigningDataSerializationOptions(JsonSerializerOptions serializerOptions)
    {
        SerializerOptions = serializerOptions;
    }
}
