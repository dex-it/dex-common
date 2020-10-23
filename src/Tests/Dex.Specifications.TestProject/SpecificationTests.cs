using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace Dex.Specifications.TestProject
{
    public class SpecificationTests
    {
        [Test]
        public void EqualSpecificationTest()
        {
            var id = Guid.NewGuid();
            var country = Guid.NewGuid();
            var employees = 5;

            var sp = new Sp<Company>()
                .Equal(c => c.Employees, employees)
                .Equal(c => c.Id, id)
                .Equal(c => c.CountryId, country);

            var expected = $@"SELECT c.""Id"", c.""CountryId"", c.""Employees"", c.""Name""{Environment.NewLine}FROM ""Companies"" AS c{Environment.NewLine}WHERE ((c.""Employees"" = @__property_0) AND (c.""Id"" = @__property_1)) AND (c.""CountryId"" = @__property_2)";

            var sql = GetSql(sp);
            Assert.AreEqual(expected, sql);
        }

        [Test]
        public void StringEqualSpecificationTest()
        {
            var companyNameFirst = "company1";

            var sp = new Sp<Company>()
                .Equal(c => c.Name, companyNameFirst);

            var expected = $@"SELECT c.""Id"", c.""CountryId"", c.""Employees"", c.""Name""{Environment.NewLine}FROM ""Companies"" AS c{Environment.NewLine}WHERE (c.""Name"" = @__property_0) OR ((c.""Name"" IS NULL) AND (@__property_0 IS NULL))";

            var sql = GetSql(sp);

            Assert.AreEqual(expected, sql);
        }

        [Test]
        public void AndSpecificationTest()
        {
            var companyIdFirst = Guid.NewGuid();
            var companyIdSecond = Guid.NewGuid();

            var sp = new Sp<Company>()
                .Equal(c => c.Id, companyIdFirst)
                .And(s => s.Equal(c => c.Id, companyIdSecond));

            var expected = $@"SELECT c.""Id"", c.""CountryId"", c.""Employees"", c.""Name""{Environment.NewLine}FROM ""Companies"" AS c{Environment.NewLine}WHERE (c.""Id"" = @__property_0) AND (c.""Id"" = @__property_1)";

            var sql = GetSql(sp);
            
            Assert.AreEqual(expected, sql);
        }

        [Test]
        public void OrSpecificationTest()
        {
            var companyIdFirst = Guid.NewGuid();
            var companyIdSecond = Guid.NewGuid();

            var sp = new Sp<Company>()
                .Equal(c => c.Id, companyIdFirst)
                .Or(s => s.Equal(c => c.Id, companyIdSecond));

            var expected = $@"SELECT c.""Id"", c.""CountryId"", c.""Employees"", c.""Name""{Environment.NewLine}FROM ""Companies"" AS c{Environment.NewLine}WHERE (c.""Id"" = @__property_0) OR (c.""Id"" = @__property_1)";

            var sql = GetSql(sp);
            
            Assert.AreEqual(expected, sql);
        }
        
        [Test]
        public void AndPlusOrSpecificationTest()
        {
            var companyName = "company1";
            var companyFirstId = Guid.NewGuid();
            var companySecondId = Guid.NewGuid();

            var sp = new Sp<Company>()
                .Like(c => c.Name, companyName)
                .And(s => s.Equal(c => c.CountryId, companyFirstId)
                                         .Or(spec => spec.Equal(c => c.CountryId, companySecondId)));

            var expected = $@"SELECT c.""Id"", c.""CountryId"", c.""Employees"", c.""Name""{Environment.NewLine}FROM ""Companies"" AS c{Environment.NewLine}WHERE (c.""Name"" LIKE @__Format_1 ESCAPE '') AND ((c.""CountryId"" = @__property_2) OR (c.""CountryId"" = @__property_3))";

            var sql = GetSql(sp);

            Assert.AreEqual(expected, sql);
        }

        private string GetSql(Sp<Company> sp)
        {
            var contextOptions = new DbContextOptionsBuilder().UseNpgsql("empty").Options;
            
            var dbContext = new DbContext(contextOptions);
            var query = dbContext.Companies.Where(sp);
            return query.ToSql();
        }
    }
}