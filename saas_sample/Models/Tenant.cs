using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace saas_sample.Models
{
    public class Tenant
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }

        public Tenant(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Id = Guid.NewGuid();
            Name = name;
        }
    }

    public interface ITenantSiteFeature
    {
        Tenant Tenant { get; }
    }

    public class TenantSiteFeature : ITenantSiteFeature
    {
        public TenantSiteFeature(Tenant tenant)
        {
            Tenant = tenant;
        }

        public Tenant Tenant { get; private set; }
    }
}
