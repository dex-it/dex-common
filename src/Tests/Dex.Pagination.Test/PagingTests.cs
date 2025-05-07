using NUnit.Framework;
using System.Linq;
using Dex.Pagination.Conditions;
using NUnit.Framework.Legacy;

namespace Dex.Pagination.Test
{
    public class PagingTests
    {
        private IQueryable<PageTestData> _data;

        [OneTimeSetUp]
        public void Setup()
        {
            _data = new[]
            {
                new PageTestData {Name = "User1", Id = 1},
                new PageTestData {Name = "User2", Id = 2},
                new PageTestData {Name = "User3", Id = 3},
                new PageTestData {Name = "User4", Id = 4}
            }.AsQueryable();
        }

        [Test]
        public void ZeroPaging()
        {
            var a1 = _data.FilterPage(0, 0).ToList();
            var a2 = _data.Take(1).ToList();

            ClassicAssert.True(a1.SequenceEqual(a2));
        }

        [Test]
        public void FirstPage()
        {
            var a1 = _data.FilterPage(1, 2).ToList();
            var a2 = _data.Take(2);

            ClassicAssert.True(a1.SequenceEqual(a2));
        }

        [Test]
        public void NotFirstPage()
        {
            var a1 = _data.FilterPage(2, 2).ToList();
            var a2 = _data.Skip(2).Take(2);

            ClassicAssert.True(a1.SequenceEqual(a2));
        }

        [Test]
        public void LimitMaxPageSizeTest()
        {
            var a1 = _data.FilterPage(new PageCondition(2, 100), 2).ToList();
            var a2 = _data.Skip(2).Take(2);

            ClassicAssert.True(a1.SequenceEqual(a2));
        }

        private record PageTestData
        {
            public int Id { get; init; }
            public string Name { get; init; }
        }
    }
}