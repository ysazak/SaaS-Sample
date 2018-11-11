using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using saas_sample.Models;

namespace saas_sample
{

    public interface ITenantStore
    {
        IEnumerable<Tenant> Tenants { get; }
        bool TryAdd(Tenant tenant);
    }
    public class TenantStore: ITenantStore
    {
        ConcurrentDictionary<Guid, Tenant> _store = new ConcurrentDictionary<Guid, Tenant>();

        public IEnumerable<Tenant> Tenants => _store.Values;

        public bool TryAdd(Tenant tenant)
        {
            return _store.TryAdd(tenant.Id, tenant);
        }
    }
}
