using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace saas_sample
{
    public class CustomRouter: MvcRouteHandler, IRouter
    {

        public new async Task RouteAsync(RouteContext context)
        {
            var host = context.HttpContext.Request.Host.Value;
            var pos = host.IndexOf(".");

            if (pos >= 0)
            {

                var subDomain = host.Substring(0, pos);
                if (subDomain.Equals("Admin", StringComparison.InvariantCultureIgnoreCase))
                {
                    context.RouteData.Values.Add("area", "Admin");
                }
                else
                {
                    context.RouteData.Values.Add("area", "Tenant");

                }
            }
            else
            {
                context.RouteData.Values.Add("area", "Website");

            }


            await base.RouteAsync(context);
        }

        public CustomRouter(IActionInvokerFactory actionInvokerFactory, IActionSelector actionSelector, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory) : base(actionInvokerFactory, actionSelector, diagnosticSource, loggerFactory)
        {
        }

        public CustomRouter(IActionInvokerFactory actionInvokerFactory, IActionSelector actionSelector, DiagnosticSource diagnosticSource, ILoggerFactory loggerFactory, IActionContextAccessor actionContextAccessor) : base(actionInvokerFactory, actionSelector, diagnosticSource, loggerFactory, actionContextAccessor)
        {
        }
    }
}
