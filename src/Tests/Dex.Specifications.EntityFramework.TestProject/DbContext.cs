using System;
using Microsoft.EntityFrameworkCore;

namespace Dex.Specifications.EntityFramework.TestProject
{
    public class DbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public DbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
    }

    public class Company
    {
        public Guid Id { get; set; }
        public int Employees { get; set; }
        public string Name { get; set; }
        public Guid CountryId { get; set; }
    }
}