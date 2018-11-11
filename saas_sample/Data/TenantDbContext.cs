using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using saas_sample.Attributes;
using saas_sample.DataModels;

namespace saas_sample.Data
{
    public class TenantDbContext : DbContext
    {
        public readonly TenantInfo TenantInfo;
        protected string ConnectionString => TenantInfo.ConnectionString;

        public TenantDbContext([NotNull]TenantInfo tenantInfo)
        {
            TenantInfo = tenantInfo;
        }

        public TenantDbContext([NotNull] TenantInfo tenantInfo, DbContextOptions options) : base(options)
        {
            TenantInfo = tenantInfo;
        }

        //Sample model mappings
        public DbSet<Contact> Contacts { get; set; }

    }
}
