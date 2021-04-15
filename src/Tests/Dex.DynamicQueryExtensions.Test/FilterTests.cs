using Dex.DynamicQueryableExtensions;
using Dex.DynamicQueryableExtensions.Data;
using NUnit.Framework;
using System.Linq;

namespace Dex.DynamicQueryExtensions.Test
{
    public class FilterTests
    {
        private IQueryable<FilterTestData> _data;

        [OneTimeSetUp]
        public void Setup()
        {
            _data = new[]
            {
                new FilterTestData {Name = "A_User", Id = 1},
                new FilterTestData {Name = "B_user", Id = 2},
                new FilterTestData {Name = "C_User", Id = 3},
                new FilterTestData {Name = "D_user", Id = 4},
            }.AsQueryable();
        }

        [Test]
        public void SimpleFilterNumber()
        {
            var a1 = _data.Filter(new IFilterCondition[]
            {
                new FilterCondition
                {
                    FieldName = "id",
                    Operation = FilterOperation.GT,
                    Value = new[] {"2"}
                }
            }).ToList();

            var a2 = _data.Where(i => i.Id > 2).ToList();

            Assert.True(a1.SequenceEqual(a2));
        }

        [Test]
        public void SimpleFilterString()
        {
            var a1 = _data.Filter(new IFilterCondition[]
            {
                new FilterCondition
                {
                    FieldName = "name",
                    Operation = FilterOperation.EQ,
                    Value = new[] { "A_User" }
                }
            }).ToList();

            var a2 = _data.Where(i => i.Name == "A_User").ToList();

            Assert.True(a1.SequenceEqual(a2));
        }

        [Test]
        public void CaseInsensitiveFilterString()
        {
            var a1 = _data.Filter(new IFilterCondition[]
            {
                new FilterCondition
                {
                    FieldName = "name",
                    Operation = FilterOperation.ILK,
                    Value = new[] {"user"}
                }
            }).ToList();

            var a2 = _data.Where(i => i.Name.ToLowerInvariant().Contains("user")).ToList();

            Assert.True(a1.SequenceEqual(a2));
        }


        [Test]
        public void CaseSensitiveFilterString()
        {
            var a1 = _data.Filter(new IFilterCondition[]
            {
                new FilterCondition
                {
                    FieldName = "name",
                    Operation = FilterOperation.LK,
                    Value = new[] {"User"}
                }
            }).ToList();

            var a2 = _data.Where(i => i.Name.Contains("User")).ToList();

            Assert.True(a1.SequenceEqual(a2));
        }

        [Test]
        public void ComplexFilter()
        {
            var a1 = _data.Filter(new IFilterCondition[]
            {
                new FilterCondition
                {
                    FieldName = "name",
                    Operation = FilterOperation.LK,
                    Value = new[] { "User" }
                },
                new FilterCondition
                {
                    FieldName = "id",
                    Operation = FilterOperation.GT,
                    Value = new[] {"2"}
                }
            }).ToList();

            var a2 = _data.Where(i => i.Name.Contains("User")
                                      && i.Id > 2).ToList();

            Assert.True(a1.SequenceEqual(a2));
        }

        private record FilterTestData
        {
            public int Id { get; init; }
            public string Name { get; init; }
            
        }
    }
}
