using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace saas_sample
{


    public class ConfigurationOptions
    {
        public string DefaultConnectionString { get; set; }
        public DbOptions Catalog { get; set; }
        public IList<DbOptions> Tenants { get; set; }
    }

    public class DbOptions
    {
        public string Name { get; set; }
        public string Identifier { get; set; }
    }
}
