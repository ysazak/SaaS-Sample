using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using saas_sample.Models;

namespace saas_sample.Extensions
{
    public static class HttpContextExtensions
    {
        private const string HttpContextTenantContext = "Tenant.Web.TenantContext";
        public static TenantContext GetTenantContext(this HttpContext context)
        {
            context.Items.TryGetValue(HttpContextTenantContext, out var tenantContext);

            return (TenantContext)tenantContext;
        }

        public static TenantSiteFeature GetTenantSiteFeature(this HttpContext context)
        {
            return context.Features.Get<TenantSiteFeature>();
        }
    }

    public class TenantContext
    {
        public TenantInfo TenantInfo { get; internal set; }

    }
}
