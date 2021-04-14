using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
                return JsonConvert.DeserializeObject<ComplexQueryCondition>(data, new StringEnumConverter(), new FilterParamConverter(), new SortParamConverter());
            }
            catch (Exception ex)
            {
                throw new InvalidDataException(ex.Message);
            }
        }

        private class FilterParamConverter : CustomCreationConverter<IFilterCondition>
        {
            public override IFilterCondition Create(Type objectType)
            {
                return new FilterCondition();
            }
        }
        private class SortParamConverter : CustomCreationConverter<ISortCondition>
        {
            public override ISortCondition Create(Type objectType)
            {
                return new SortCondition();
            }
        }
    }
}