using System.Linq;
using Dex.Pagination.Conditions;
using Dex.TestDomain;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Dex.Pagination.Test
{
    public class SortTests
    {
        [Test]
        public void SqlGenerateWithOrderByParams_Test_Positive()
        {
            using var dbContext = DbContext.GetDbContextWithEmptyConnectionString();

            var queryActual = dbContext.Employees.OrderBy(x => x.Company.CreatedUtc);
            var sqlActual = queryActual.ToSql();

            var expectedQuery = dbContext.Employees.OrderByParams(new OrderCondition("Company.CreatedUtc"));
            var expectedSql = expectedQuery.ToSql();

            ClassicAssert.AreEqual(expectedSql, sqlActual);
        }

        [Test]
        public void OrderByParamTest2()
        {
            var array = new[]
            {
                new SortTestData {Name = "Max", Number = 1},
                new SortTestData {Name = "Max2", Number = 2},
                new SortTestData {Name = "Max3", Number = 3},
                new SortTestData {Name = "Max4", Number = 4},
            };

            var a1 = array.AsQueryable().OrderByParams(new OrderCondition(nameof(SortTestData.Number), true));
            var a2 = array.OrderByDescending(x => x.Number);

            ClassicAssert.True(a1.SequenceEqual(a2));
        }

        [Test]
        public void OrderByEmptyParamTest()
        {
            var array = new[]
            {
                new SortTestData {Name = "Max", Number = 1},
                new SortTestData {Name = "Max2", Number = 2},
                new SortTestData {Name = "Max3", Number = 3},
                new SortTestData {Name = "Max4", Number = 4},
            };

            var a1 = array.AsQueryable().ApplyCondition(new QueryCondition());
            ClassicAssert.True(array.SequenceEqual(a1));
        }

        private record SortTestData
        {
            public string Name { get; init; }
            public int Number { get; init; }
        }
    }
}