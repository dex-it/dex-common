using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Dex.TestDomain
{
    public class DbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public DbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Employee> Employees { get; set; }

        public static DbContext GetDbContextWithEmptyConnectionString()
        {
            var contextOptions = new DbContextOptionsBuilder().UseNpgsql("Host=fake;Database=fake;Username=fake;Password=fake").Options;

            var dbContext = new DbContext(contextOptions);
            return dbContext;
        }
    }

    public class Company
    {
        public Guid Id { get; set; }
        public int Employees { get; set; }
        public string Name { get; set; }
        public Guid CountryId { get; set; }
        public DateTime CreatedUtc { get; set; }

        public ICollection<Employee> CollectionEmployees { get; set; }
    }

    public class Employee
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedUtc { get; set; }

        public Guid CompanyId { get; set; }
        public Company Company { get; set; }
    }
}