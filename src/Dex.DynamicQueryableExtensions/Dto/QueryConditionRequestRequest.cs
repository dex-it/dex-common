using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dex.DynamicQueryableExtensions.Conditions;
using Dex.DynamicQueryableExtensions.Data;

namespace Dex.DynamicQueryableExtensions.Dto
{
    public record QueryConditionRequestRequest
    {
        public string EncodedFilter { get; init; }

        public IQueryCondition DecodeFilter()
        {
            if (string.IsNullOrWhiteSpace(EncodedFilter))
                return null;

            try
            {
                var data = Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(EncodedFilter)));
                return JsonSerializer.Deserialize<QueryCondition>(data, new JsonSerializerOptions
                {
                    Converters =
                    {
                        new JsonStringEnumConverter(),
                        new FilterConditionConverter(),
                        new SortConditionConverter()
                    }
                });
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
                return JsonSerializer.Deserialize<OrderCondition>(ref reader, options);
            }

            public override void Write(Utf8JsonWriter writer, IOrderCondition value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }
        }

        private class FilterConditionConverter : JsonConverter<IFilterCondition>
        {
            public override IFilterCondition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return JsonSerializer.Deserialize<FilterCondition>(ref reader, options);
            }

            public override void Write(Utf8JsonWriter writer, IFilterCondition value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }
        }

        #endregion
    }
}