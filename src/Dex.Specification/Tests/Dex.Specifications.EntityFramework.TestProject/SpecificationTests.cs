using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Dex.Specifications.Extensions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Dex.TestDomain;
using DbContext = Dex.TestDomain.DbContext;

namespace Dex.Specifications.EntityFramework.TestProject
{
    public class SpecificationTests
    {
        [Test]
        public void EqualSpecificationTest()
        {
            var id = Guid.NewGuid();
            var country = Guid.NewGuid();
            var employees = 5;

            var sp = Sp<Company>
                .Equal(c => c.Employees, employees)
                .AndEqual(c => c.Id, id)
                .AndEqual(c => c.CountryId, country);

            Expression<Func<Company, bool>> query = c => c.Employees == employees && c.Id == id && c.CountryId == country;

            var specificationSql = GetSql(sp);
            var expressionSql = GetSql(query);

            Assert.AreEqual(expressionSql, specificationSql);
        }

        [Test]
        public void StringEqualSpecificationTest()
        {
            var companyNameFirst = "company1";

            var sp = Sp<Company>
                .Equal(c => c.Name, companyNameFirst);

            Expression<Func<Company, bool>> query = c => c.Name == companyNameFirst;

            var specificationSql = GetSql(sp);
            var expressionSql = GetSql(query);

            Assert.AreEqual(specificationSql, expressionSql);
        }

        [Test]
        public void AndExtensionSpecificationTest()
        {
            var companyIdFirst = Guid.NewGuid();
            var companyIdSecond = Guid.NewGuid();

            var sp = Sp<Company>
                .Equal(c => c.Id, companyIdFirst)
                .And(s => s.AndEqual(c => c.Id, companyIdSecond));

            Expression<Func<Company, bool>> query = c => c.Id == companyIdFirst && c.Id == companyIdSecond;

            var specificationSql = GetSql(sp);
            var expressionSql = GetSql(query);

            Assert.AreEqual(specificationSql, expressionSql);
        }

        [Test]
        public void AndSpecificationTest()
        {
            var companyIdFirst = Guid.NewGuid();
            var companyIdSecond = Guid.NewGuid();

            var sp = Sp<Company>
                .Equal(c => c.Id, companyIdFirst)
                .And(Sp<Company>.Equal(c => c.Id, companyIdSecond));

            Expression<Func<Company, bool>> query = c => c.Id == companyIdFirst && c.Id == companyIdSecond;

            var specificationSql = GetSql(sp);
            var expressionSql = GetSql(query);

            Assert.AreEqual(specificationSql, expressionSql);
        }

        [Test]
        public void OrExtensionSpecificationTest()
        {
            var companyIdFirst = Guid.NewGuid();
            var companyIdSecond = Guid.NewGuid();

            var sp = Sp<Company>
                .Equal(c => c.Id, companyIdFirst)
                .Or(s => s.AndEqual(c => c.Id, companyIdSecond));

            Expression<Func<Company, bool>> query = c => c.Id == companyIdFirst || c.Id == companyIdSecond;

            var specificationSql = GetSql(sp);
            var expressionSql = GetSql(query);

            Assert.AreEqual(specificationSql, expressionSql);
        }

        [Test]
        public void OrSpecificationTest()
        {
            var companyIdFirst = Guid.NewGuid();
            var companyIdSecond = Guid.NewGuid();

            var sp = Sp<Company>
                .Equal(c => c.Id, companyIdFirst)
                .Or(Sp<Company>.Equal(c => c.Id, companyIdSecond));

            Expression<Func<Company, bool>> query = c => c.Id == companyIdFirst || c.Id == companyIdSecond;

            var specificationSql = GetSql(sp);
            var expressionSql = GetSql(query);

            Assert.AreEqual(specificationSql, expressionSql);
        }

        [Test]
        public void MultipleOrSpecificationTest()
        {
            var countryId = Guid.NewGuid();
            var id = Guid.NewGuid();
            var employees = 3;

            var sp = Sp<Company>
                .Equal(c => c.CountryId, countryId)
                .Or(s => s.AndEqual(c => c.Id, id)
                    .Or(s2 => s2.AndEqual(c => c.Employees, employees)));

            Expression<Func<Company, bool>> query = c => c.CountryId == countryId || (c.Id == id || c.Employees == employees);

            var specificationSql = GetSql(sp);
            var expressionSql = GetSql(query);

            Assert.AreEqual(specificationSql, expressionSql);
        }

        [Test]
        public void AndPlusOrSpecificationTest()
        {
            var companyName = "company1";
            var companyFirstId = Guid.NewGuid();
            var companySecondId = Guid.NewGuid();

            var sp = Sp<Company>
                .Like(c => c.Name, companyName)
                .And(s => s.AndEqual(c => c.Id, companyFirstId)
                    .Or(s2 => s2.AndEqual(c => c.Id, companySecondId)));

            Expression<Func<Company, bool>> query = c => EF.Functions.Like(c.Name, $"%{companyName}%") && (c.Id == companyFirstId || c.Id == companySecondId);

            var specificationSql = GetSql(sp);
            var expressionSql = GetSql(query);

            Assert.AreEqual(specificationSql, expressionSql);
        }

        [Test]
        public void NotCustomSpecificationTest()
        {
            var companyId = Guid.NewGuid();
            var countryId = Guid.NewGuid();
            var employees = 5;

            var specification = new EfEqualSpecification<Company, Guid>(c => c.CountryId, countryId)
                .AndEqual(c => c.Employees, employees);

            var sp = new EfEqualSpecification<Company, Guid>(c => c.Id, companyId).Not(specification);
            Expression<Func<Company, bool>> query = c => c.Id == companyId && !(c.CountryId == countryId && c.Employees == employees);

            var specificationSql = GetSql(sp);
            var expressionSql = GetSql(query);

            Assert.AreEqual(specificationSql, expressionSql);
        }

        [Test]
        public void NotExtensionSpecificationTest()
        {
            var companyId = Guid.NewGuid();

            var sp = Sp<Company>
                .Not(s => s.AndEqual(c => c.Id, companyId));

            Expression<Func<Company, bool>> query = c => c.Id != companyId;

            var specificationSql = GetSql(sp);
            var expressionSql = GetSql(query);

            Assert.AreEqual(specificationSql, expressionSql);
        }

        [Test]
        public void NotSimpleSpecificationTest()
        {
            var companyId = Guid.NewGuid();

            var sp = new NotSpecification<Company>(new EfEqualSpecification<Company, Guid>(c => c.Id, companyId));

            Expression<Func<Company, bool>> query = c => c.Id != companyId;

            var specificationSql = GetSql(sp);
            var expressionSql = GetSql(query);

            Assert.AreEqual(specificationSql, expressionSql);
        }

        [Test]
        public void OrNotSpecificationTest()
        {
            var companyId = Guid.NewGuid();
            var countryId = Guid.NewGuid();

            var sp = Sp<Company>
                .Equal(s => s.Id, companyId)
                .Or(s => s.Not(s2 => s2.AndEqual(c => c.CountryId, countryId)));

            Expression<Func<Company, bool>> query = c => c.Id == companyId || c.CountryId != countryId;

            var specificationSql = GetSql(sp);
            var expressionSql = GetSql(query);

            Assert.AreEqual(specificationSql, expressionSql);
        }

        [Test]
        public void AndNotSpecificationTest()
        {
            var companyId = Guid.NewGuid();
            var countryId = Guid.NewGuid();

            var sp = Sp<Company>
                .Equal(s => s.Id, companyId)
                .And(s => s.Not(s2 => s2.AndEqual(c => c.CountryId, countryId)));

            Expression<Func<Company, bool>> query = c => c.Id == companyId && c.CountryId != countryId;

            var specificationSql = GetSql(sp);
            var expressionSql = GetSql(query);

            Assert.AreEqual(specificationSql, expressionSql);
        }

        private static string GetSql(Expression<Func<Company, bool>> expression)
        {
            using var dbContext = DbContext.GetDbContextWithEmptyConnectionString();
            var query = dbContext.Companies.Where(expression);
            var sql = query.ToSql();

            var regex = new Regex(@"\B@\w+");

            var match = regex.Match(sql);

            var counter = 0;
            while (match.Success)
            {
                sql = sql.Replace(match.Value, $"paramProperty{counter++}");

                match = regex.Match(sql);
            }

            return sql;
        }
    }
}