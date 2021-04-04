using System.Linq;
using Dex.Pagination.Data;
using Dex.TestDomain;
using Dex.TestHelper;
using NUnit.Framework;

namespace Dex.Pagination.TestProject
{
    public class PaginationTests
    {
        [Test]
        public void SqlGenerateWithOrderByParams_Test_Positive()
        {
            using var dbContext = DbContext.GetDbContextWithEmptyConnectionString();

            var queryActual = dbContext.Employees.OrderBy(x => x.Company.CreatedUtc);
            var sqlActual = queryActual.ToSql();

            var expectedQuery = dbContext.Employees.OrderByParams(new SortParam
            {
                FieldName = "Company.CreatedUtc",
                IsDesc = false
            });
            var expectedSql = expectedQuery.ToSql();

            Assert.AreEqual(expectedSql, sqlActual);
        }
    }
}