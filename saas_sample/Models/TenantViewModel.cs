using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace saas_sample.Models
{
    public class TenantViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string SiteUrl { get; set; }
    }
}
