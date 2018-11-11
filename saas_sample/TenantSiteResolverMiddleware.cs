using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using saas_sample.Extensions;
using saas_sample.Models;

namespace saas_sample
{
    public class TenantSiteResolverMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public TenantSiteResolverMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger(typeof(TenantSiteResolverMiddleware).ToString());
        }

        public async Task Invoke(HttpContext context)
        {
            using (_logger.BeginScope("v"))
            {
                var host = context.Request.Host.Value;
                var pos = host.IndexOf(".");

                if (pos >= 0)
                {

                    var subDomain = host.Substring(0, pos);

                    var tenant = new Tenant(subDomain);

                    _logger.LogInformation(string.Format("Resolved tenant. Current tenant: {0}", tenant.Id));

                    var tenantFeature = new TenantSiteFeature(tenant);
                    context.Features.Set<ITenantSiteFeature>(tenantFeature);
                    context.Items.Add("Tenant.Web.TenantContext", new TenantContext
                    {
                        TenantInfo = new TenantInfo
                        {
                            Name = subDomain,
                            ConnectionString = String.Format("Server=.;Database=saas_{0};User Id=sa;Password=Password1;", subDomain)
                        }
                    });
                }

                await _next(context);
            }
        }
    }

    public static class TenantSiteResolverMiddlewareExtensions
    {
        public static void UseTenantSiteResolver(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<TenantSiteResolverMiddleware>();
        }
    }
}
