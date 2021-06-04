using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Dex.Cap.Ef.Tests
{
    public class Tests
    {
        private string _dbTest;

        [OneTimeSetUp]
        public async Task Setup()
        {
            _dbTest = "db_test_" + Guid.NewGuid().ToString("N");
            var db = new TestDbContext(_dbTest);
            await db.Database.MigrateAsync();
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            var db = new TestDbContext(_dbTest);
            await db.Database.EnsureDeletedAsync();
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}