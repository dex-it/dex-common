using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dex.Pagination.Conditions;
// ReSharper disable UnusedType.Global

namespace Dex.Pagination.Dto
{
    public record QueryConditionRequest
    {
        public string EncodedFilterUriEscaped { get; init; } = string.Empty;

        public IQueryCondition DecodeFilter()
        {
            if (string.IsNullOrWhiteSpace(EncodedFilterUriEscaped))
                return new QueryCondition();

            try
            {
                var data = Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(EncodedFilterUriEscaped)));
                return JsonSerializer.Deserialize<QueryCondition>(data, new JsonSerializerOptions
                {
                    Converters =
                    {
                        new JsonStringEnumConverter(),
                        new SortConditionConverter()
                    }
                }) ?? new QueryCondition();
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(ex.Message);
            }
        }

        #region deserializer

        private class SortConditionConverter : JsonConverter<IOrderCondition>
        {
            public override IOrderCondition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return JsonSerializer.Deserialize<OrderCondition>(ref reader, options) ?? new OrderCondition();
            }

            public override void Write(Utf8JsonWriter writer, IOrderCondition value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }
        }

        #endregion
    }
}