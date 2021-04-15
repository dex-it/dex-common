using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dex.DynamicQueryableExtensions.Data
{
    public record QueryFilterEncodedRequest : IQueryFilterEncoded
    {
        public string EncodedFilter { get; set; }

        public IComplexQueryCondition DecodeFilter()
        {
            if (string.IsNullOrWhiteSpace(EncodedFilter))
                return null;

            try
            {
                var data = Encoding.UTF8.GetString(Convert.FromBase64String(EncodedFilter));
                return JsonSerializer.Deserialize<ComplexQueryCondition>(data, new JsonSerializerOptions()
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

        private class SortConditionConverter : JsonConverter<ISortCondition>
        {
            public override ISortCondition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return JsonSerializer.Deserialize<SortCondition>(ref reader, options);
            }

            public override void Write(Utf8JsonWriter writer, ISortCondition value, JsonSerializerOptions options)
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
    }
}