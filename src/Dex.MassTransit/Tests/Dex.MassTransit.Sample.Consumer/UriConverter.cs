using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dex.MassTransit.Sample.Consumer
{
    public class UriConverter : JsonConverter<Uri?>
    {
        public override Uri? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? uriString = reader.GetString();
            if (Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out Uri? value))
            {
                return value;
            }

            return null;
        }

        public override void Write(Utf8JsonWriter writer, Uri? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.OriginalString);                
            }
        }
    }
}