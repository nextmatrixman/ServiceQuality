using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceQuality.Models
{
    public class ServiceDbContext : DbContext
    {
        public DbSet<Service> Services { get; set; }
        public DbSet<Result> Results { get; set; }

        //public ServiceDbContext() { }

        public ServiceDbContext(DbContextOptions options) : base(options) { }
    }
}

