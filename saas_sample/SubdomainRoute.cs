using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace saas_sample
{
    public class SubDomainRoute : Route
    {

        private static readonly string w3 = "www.";
        private static readonly string w3Regex = "^www.";
        private static readonly IDictionary<string, Type> _unavailableConstraints = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "datetime", typeof(DateTimeRouteConstraint) },
            { "decimal", typeof(DecimalRouteConstraint) },
            { "double", typeof(DoubleRouteConstraint) },
            { "float", typeof(FloatRouteConstraint) },
        };

        private readonly IDictionary<string, IRouteConstraint> constraintsWithSubdomainConstraint;

        public string[] Hostnames { get; private set; }

        public string Subdomain { get; private set; }

        public RouteTemplate SubdomainParsed { get; private set; }

        public SubDomainRoute(string[] hostnames, string subdomain, IRouter target, string routeName, string routeTemplate, RouteValueDictionary defaults, IDictionary<string, object> constraints,
           RouteValueDictionary dataTokens, IInlineConstraintResolver inlineConstraintResolver, IOptions<RouteOptions> routeOptions)
           : base(target, routeName, routeTemplate, defaults, constraints, dataTokens, inlineConstraintResolver)
        {
            Hostnames = hostnames;

            if (string.IsNullOrEmpty(subdomain))
            {
                return;
            }

            SubdomainParsed = TemplateParser.Parse(subdomain);
            Constraints = GetConstraints(inlineConstraintResolver, TemplateParser.Parse(routeTemplate), constraints);

            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/routing
            constraintsWithSubdomainConstraint = GetConstraints(ConstraintResolver, SubdomainParsed, null);

            Defaults = GetDefaults(SubdomainParsed, Defaults);
            //Defaults = GetDefaults(TemplateParser.Parse(routeTemplate), defaults);


            if (constraintsWithSubdomainConstraint.Count == 1)
            {
                Subdomain = RemoveConstraint(subdomain);
            }
            else
            {
                Subdomain = subdomain;
            }

            if (IsParameterName(Subdomain))
            {
                if (constraintsWithSubdomainConstraint.Any(x => _unavailableConstraints.Values.Contains(x.Value.GetType()) && x.Key == ParameterNameFrom(Subdomain)))
                {
                    throw new ArgumentException($"Constraint invalid on subdomain! " +
                        $"Constraints: {string.Join(Environment.NewLine, _unavailableConstraints.Select(x => x.Key))}{Environment.NewLine}are unavailable for subdomain.");
                }

                foreach (var c in Constraints)
                {
                    constraintsWithSubdomainConstraint.Add(c);
                }

                if (Constraints.Keys.Contains(ParameterNameFrom(subdomain)))
                {
                    Constraints.Remove(ParameterNameFrom(subdomain));
                }
            }
        }

        public override Task RouteAsync(RouteContext context)
        {
            var host = context.HttpContext.Request.Host.Value;

            string foundHostname = GetHostname(host);

            if (foundHostname == null && Subdomain != null)
            {
                return Task.CompletedTask;
            }

            if (Subdomain == null)
            {
                if (foundHostname != null)
                {
                    return Task.CompletedTask;
                }

                return base.RouteAsync(context);
            }

            var subdomain = host.Substring(0, host.IndexOf(GetHostname(host)) - 1);

            if (!IsParameterName(Subdomain) && subdomain.ToLower() != Subdomain.ToLower())
            {
                return Task.CompletedTask;
            }

            var parsedTemplate = TemplateParser.Parse(Subdomain);
            //that's for overriding default for subdomain
            if (IsParameterName(Subdomain) &&
                Defaults.ContainsKey(ParameterNameFrom(Subdomain)) &&
                !context.RouteData.Values.ContainsKey(ParameterNameFrom(Subdomain)))
            {
                context.RouteData.Values.Add(ParameterNameFrom(Subdomain), subdomain);
            }

            if (IsParameterName(Subdomain) &&
                constraintsWithSubdomainConstraint.ContainsKey(ParameterNameFrom(Subdomain)))
            {
                if (!RouteConstraintMatcher.Match(
                        constraintsWithSubdomainConstraint,
                        new RouteValueDictionary
                        {
                            {  ParameterNameFrom(Subdomain), subdomain }
                        },
                        context.HttpContext,
                        this,
                        RouteDirection.IncomingRequest,
                        context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(RouteConstraintMatcher).FullName)))
                {
                    return Task.CompletedTask;
                }
            }


            return base.RouteAsync(context);
        }

        protected override Task OnRouteMatched(RouteContext context)
        {
            if (Subdomain == null)
            {
                return base.OnRouteMatched(context);
            }
            var host = context.HttpContext.Request.Host.Value;
            var subdomain = host.Substring(0, host.IndexOf(GetHostname(host)) - 1);
            var routeData = new RouteData(context.RouteData);

            if (IsParameterName(Subdomain))
            {
                //override default
                if (Defaults.ContainsKey(ParameterNameFrom(Subdomain)) && routeData.Values.ContainsKey(ParameterNameFrom(Subdomain)))
                {
                    routeData.Values[ParameterNameFrom(Subdomain)] = subdomain;
                }
                //or add this which will allow to get value from example view via RouteData
                else if (!routeData.Values.ContainsKey(ParameterNameFrom(Subdomain)))
                {
                    routeData.Values.Add(ParameterNameFrom(Subdomain), subdomain);
                }
            }

            context.RouteData = routeData;

            return base.OnRouteMatched(context);
        }

        public override VirtualPathData GetVirtualPath(VirtualPathContext context)
        {
            if (Subdomain == null)
            {
                //if route is without subdomain and we are on host without subdomain use base method
                //we don't need whole URL for such case
                if (GetHostname(context.HttpContext.Request.Host.Value) == null)
                {
                    return base.GetVirtualPath(context);
                }

                return GetVirtualPath(context, context.Values, BuildUrl(context));
            }

            var subdomainParameter = IsParameterName(Subdomain) ? ParameterNameFrom(Subdomain) : Subdomain;

            var containsSubdomainParameter = context.Values.ContainsKey(subdomainParameter);

            var defaultsContainsSubdomain = this.Defaults.ContainsKey(subdomainParameter);

            if (IsParameterName(Subdomain))
            {
                var sParameter = ParameterNameFrom(Subdomain);
                if (context.Values.ContainsKey(sParameter))
                {
                    return ParameterSubdomain(context, context.Values[subdomainParameter].ToString());
                }
                else if (this.Defaults.ContainsKey(sParameter))
                {
                    return ParameterSubdomain(context, this.Defaults[sParameter].ToString());
                }
            }
            else
            {
                if (!IsParameterName(Subdomain))
                {
                    return StaticSubdomain(context, subdomainParameter);
                }
            }

            return null;
        }

        private string GetHostname(string host)
        {
            var nonW3Host = System.Text.RegularExpressions.Regex.Replace(host, w3Regex, "");
            foreach (var hostname in Hostnames)
            {
                if (!nonW3Host.EndsWith(hostname) || nonW3Host == hostname)
                {
                    continue;
                }

                return hostname;
            }

            return null;
        }

        private string ParameterNameFrom(string value)
        {
            return value.Substring(1, value.LastIndexOf("}") - 1);
        }

        private bool IsParameterName(string value)
        {
            if (value.StartsWith("{") && value.EndsWith("}"))
                return true;

            return false;
        }

        private bool EqualsToUrlParameter(string value, string urlParameter)
        {
            var param = ParameterNameFrom(urlParameter);

            return value.Equals(param);
        }

        private string CreateVirtualPathString(VirtualPathData vpd, RouteValueDictionary values)
        {
            var vp = vpd.VirtualPath;

            if (vp.Contains('?'))
            {
                return string.Format("{0}&{1}={2}", vp, Subdomain, values[Subdomain]);
            }
            else
            {
                return string.Format("{0}?{1}={2}", vp, Subdomain, values[Subdomain]);
            }
        }

        private AbsolutPathData StaticSubdomain(VirtualPathContext context, string subdomainParameter)
        {
            var hostBuilder = BuilSubdomaindUrl(context, subdomainParameter);

            return GetVirtualPath(context, context.Values, hostBuilder);
        }

        private AbsolutPathData ParameterSubdomain(VirtualPathContext context, string subdomainValue)
        {
            var hostBuilder = BuilSubdomaindUrl(context, subdomainValue);

            //we have to remove our subdomain so it will not be added as query string while using GetVirtualPath method
            var values = new RouteValueDictionary(context.Values);
            values.Remove(ParameterNameFrom(Subdomain));

            return GetVirtualPath(context, values, hostBuilder);
        }

        private AbsolutPathData GetVirtualPath(VirtualPathContext context, RouteValueDictionary routeValues, StringBuilder hostBuilder)
        {
            var path = base.GetVirtualPath(new VirtualPathContext(context.HttpContext, context.AmbientValues, routeValues));

            if (path == null) { return null; }

            return new AbsolutPathData(this, path.VirtualPath, hostBuilder.ToString(), context.HttpContext.Request.Scheme);
        }

        private StringBuilder BuildUrl(VirtualPathContext context)
        {
            return BuildAbsoluteUrl(context, (hostBuilder, host) =>
            {
                hostBuilder
                    .Append(host);
            });
        }

        private StringBuilder BuilSubdomaindUrl(VirtualPathContext context, string subdomainValue)
        {
            return BuildAbsoluteUrl(context, (hostBuilder, host) =>
            {
                hostBuilder
                    .Append(subdomainValue)
                    .Append(".")
                    .Append(host);
            });
        }

        private StringBuilder BuildAbsoluteUrl(VirtualPathContext context, Action<StringBuilder, string> buildAction)
        {
            string foundHostname = GetHostname(context.HttpContext.Request.Host.Value);

            string host = System.Text.RegularExpressions.Regex.Replace(context.HttpContext.Request.Host.Value, w3Regex, "");
            if (!string.IsNullOrEmpty(foundHostname))
            {
                var subdomain = host.Substring(0, host.IndexOf(foundHostname) - 1);

                if (!string.IsNullOrEmpty(subdomain))
                {
                    host = foundHostname;
                }
            }

            var hostBuilder = new StringBuilder();

            if (context.HttpContext.Request.Host.Value.StartsWith(w3))
            {
                hostBuilder.Append(w3);
            }

            buildAction(hostBuilder, host);

            return hostBuilder;
        }

        private string RemoveConstraint(string segment)
        {
            return $"{segment.Substring(0, segment.IndexOf(':'))}}}";
        }
    }

    public class AbsolutPathData : VirtualPathData
    {
        /// <summary>
        /// Gets or sets the host that was generated from the <see cref="VirtualPathData.Router"/>.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the protocol that was generated from the <see cref="VirtualPathData.Router"/>.
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbsolutPathData"/>.
        /// </summary>
        /// <param name="router">The object that is used to generate the URL.</param>
        /// <param name="virtualPath">The generated URL.</param>
        /// <param name="host">The generated host.</param>
        /// <param name="protocol">The generated protocol.</param>
        public AbsolutPathData(IRouter router, string virtualPath, string host, string protocol)
            : base(router, virtualPath)
        {
            Host = host;
            Protocol = protocol;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbsolutPathData"/>
        /// </summary>
        /// <param name="router">The object that is used to generate the URL.</param>
        /// <param name="virtualPath">The generated URL.</param>
        /// <param name="dataTokens">The collection of custom values.</param>
        /// <param name="host">The generated host.</param>
        /// <param name="protocol">The generated protocol.</param>
        public AbsolutPathData(IRouter router, string virtualPath, RouteValueDictionary dataTokens, string host, string protocol)
            : base(router, virtualPath, dataTokens)
        {
            Host = host;
            Protocol = protocol;
        }
    }

    public static class RoutingServiceCollectionSubdomainExtensions
    {
        /// <summary>
        /// Injects subdomain dependencies into <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">Services to inject to.</param>
        /// <returns>Returns updated instance of <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddSubdomains(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton(typeof(IUrlHelperFactory), typeof(SubdomainUrlHelperFactory));

            return services;
        }
    }
    public class SubdomainUrlHelper : UrlHelper, IUrlHelper
    {
        // Perf: Reuse the RouteValueDictionary across multiple calls of Action for this UrlHelper
        private readonly RouteValueDictionary _routeValueDictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubdomainUrlHelper"/> class using the specified
        /// <paramref name="actionContext"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/> for the current request.</param>
        public SubdomainUrlHelper(ActionContext actionContext) : base(actionContext)
        {
            _routeValueDictionary = new RouteValueDictionary();
        }

        /// <inheritdoc />
        public override string Action(UrlActionContext actionContext)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            var valuesDictionary = GetValuesDictionary(actionContext.Values);

            if (actionContext.Action == null)
            {
                if (!valuesDictionary.ContainsKey("action") &&
                    AmbientValues.TryGetValue("action", out object action))
                {
                    valuesDictionary["action"] = action;
                }
            }
            else
            {
                valuesDictionary["action"] = actionContext.Action;
            }

            if (actionContext.Controller == null)
            {
                if (!valuesDictionary.ContainsKey("controller") &&
                    AmbientValues.TryGetValue("controller", out object controller))
                {
                    valuesDictionary["controller"] = controller;
                }
            }
            else
            {
                valuesDictionary["controller"] = actionContext.Controller;
            }

            var pathData = GetVirtualPathData(routeName: null, values: valuesDictionary);
            if (pathData is AbsolutPathData)
            {
                var absolutePathData = pathData as AbsolutPathData;

                if (string.Equals(absolutePathData.Host, HttpContext.Request.Host.Value, StringComparison.CurrentCultureIgnoreCase) && absolutePathData.Protocol == HttpContext.Request.Scheme)
                {
                    return GenerateUrl(null, null, pathData, actionContext.Fragment);
                }
                //we don't support changing protocol for subdomain
                return GenerateUrl(absolutePathData.Protocol, absolutePathData.Host, pathData, actionContext.Fragment);
            }
            return GenerateUrl(actionContext.Protocol, actionContext.Host, pathData, actionContext.Fragment);
        }

        /// <inheritdoc />
        public override string RouteUrl(UrlRouteContext routeContext)
        {
            if (routeContext == null)
            {
                throw new ArgumentNullException(nameof(routeContext));
            }

            var valuesDictionary = routeContext.Values as RouteValueDictionary ?? GetValuesDictionary(routeContext.Values);
            var pathData = GetVirtualPathData(routeContext.RouteName, valuesDictionary);
            if (pathData is AbsolutPathData)
            {
                //we don't support changing protocol for subdomain
                return GenerateUrl(((AbsolutPathData)pathData).Protocol, ((AbsolutPathData)pathData).Host, pathData, routeContext.Fragment);
            }
            return GenerateUrl(routeContext.Protocol, routeContext.Host, pathData, routeContext.Fragment);
        }

        /// <inheritdoc />
        public override string Content(string contentPath)
        {
            //todo: body

            return base.Content(contentPath);
        }

        /// <inheritdoc />
        public override string Link(string routeName, object values)
        {
            //todo: body
            return base.Link(routeName, values);
        }

        private RouteValueDictionary GetValuesDictionary(object values)
        {
            // Perf: RouteValueDictionary can be cast to IDictionary<string, object>, but it is
            // special cased to avoid allocating boxed Enumerator.
            var routeValuesDictionary = values as RouteValueDictionary;
            if (routeValuesDictionary != null)
            {
                _routeValueDictionary.Clear();
                foreach (var kvp in routeValuesDictionary)
                {
                    _routeValueDictionary.Add(kvp.Key, kvp.Value);
                }

                return _routeValueDictionary;
            }

            var dictionaryValues = values as IDictionary<string, object>;
            if (dictionaryValues != null)
            {
                _routeValueDictionary.Clear();
                foreach (var kvp in dictionaryValues)
                {
                    _routeValueDictionary.Add(kvp.Key, kvp.Value);
                }

                return _routeValueDictionary;
            }

            return new RouteValueDictionary(values);
        }
    }

    public class SubdomainUrlHelperFactory : IUrlHelperFactory
    {
        /// <summary>
        /// Gets an <see cref="IUrlHelper"/> for the request associated with context.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/> associated with the current request.</param>
        /// <returns>An <see cref="IUrlHelper"/> for the request associated with context</returns>
        public IUrlHelper GetUrlHelper(ActionContext context)
        {
            var httpContext = context.HttpContext;

            if (httpContext == null)
            {
                throw new ArgumentException(nameof(ActionContext.HttpContext));
            }

            if (httpContext.Items == null)
            {
                throw new ArgumentException(nameof(HttpContext.Items));
            }

            // Perf: Create only one UrlHelper per context
            if (httpContext.Items.TryGetValue(typeof(IUrlHelper), out object value) && value is IUrlHelper)
            {
                return (IUrlHelper)value;
            }

            var urlHelper = new SubdomainUrlHelper(context);
            httpContext.Items[typeof(IUrlHelper)] = urlHelper;

            return urlHelper;
        }
    }

    public static class MapSubdomainRouteRouteBuilderExtensions
    {
        /// <summary>
        /// Adds a subdomain route to the IRouteBuilder with the specified hostnames, name and template.
        /// </summary>
        /// <param name="routeBuilder">The IRouteBuilder to add the route to.</param>
        /// <param name="hostnames">Hostnames which should be recognized by application.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="subdomain">The subdomain pattern of the route.</param>
        /// <param name="template">The URL pattern of the route.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IRouteBuilder MapSubdomainRoute(this IRouteBuilder routeBuilder, string[] hostnames, string name, string subdomain, string template)
        {
            return MapSubdomainRoute(routeBuilder, hostnames, name, subdomain, template, defaults: null);
        }

        /// <summary>
        /// Adds a subdomain route to the IRouteBuilder with the specified hostnames, name, template and defaults.
        /// </summary>
        /// <param name="routeBuilder">The IRouteBuilder to add the route to.</param>
        /// <param name="hostnames">Hostnames which should be recognized by application.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="subdomain">The subdomain pattern of the route.</param>
        /// <param name="template">The URL pattern of the route.</param>
        /// <param name="defaults">An object that contains default values for route parameters. The object's properties represent the names and values of the default values.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IRouteBuilder MapSubdomainRoute(this IRouteBuilder routeBuilder, string[] hostnames, string name, string subdomain, string template, object defaults)
        {
            return MapSubdomainRoute(routeBuilder, hostnames, name, subdomain, template, defaults, constraints: null);
        }

        /// <summary>
        /// Adds a subdomain route to the IRouteBuilder with the specified hostnames, name, template and defaults.
        /// </summary>
        /// <param name="routeBuilder">The IRouteBuilder to add the route to.</param>
        /// <param name="hostnames">Hostnames which should be recognized by application.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="subdomain">The subdomain pattern of the route.</param>
        /// <param name="template">The URL pattern of the route.</param>
        /// <param name="defaults">An object that contains default values for route parameters. The object's properties represent the names and values of the default values.</param>
        /// <param name="constraints">An object that contains constraints for the route. The object's properties represent the names and values of the constraints.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IRouteBuilder MapSubdomainRoute(this IRouteBuilder routeBuilder, string[] hostnames, string name, string subdomain, string template, object defaults, object constraints)
        {
            return MapSubdomainRoute(routeBuilder, hostnames, name, subdomain, template, defaults, constraints, dataTokens: null);
        }

        /// <summary>
        /// Adds a subdomain route to the IRouteBuilder with the specified hostnames, name, template and defaults.
        /// </summary>
        /// <param name="routeBuilder">The IRouteBuilder to add the route to.</param>
        /// <param name="hostnames">Hostnames which should be recognized by application.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="subdomain">The subdomain pattern of the route.</param>
        /// <param name="template">The URL pattern of the route.</param>
        /// <param name="defaults">An object that contains default values for route parameters. The object's properties represent the names and values of the default values.</param>
        /// <param name="constraints">An object that contains constraints for the route. The object's properties represent the names and values of the constraints.</param>
        /// <param name="dataTokens">An object that contains data tokens for the route. The object's properties represent the names and values of the data tokens.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IRouteBuilder MapSubdomainRoute(this IRouteBuilder routeBuilder, string[] hostnames, string name, string subdomain, string template, object defaults, object constraints, object dataTokens)
        {
            var routeOptions = (IOptions<RouteOptions>)routeBuilder.ServiceProvider.GetService(typeof(IOptions<RouteOptions>));
            routeBuilder.Routes.Add(new SubDomainRoute(hostnames, subdomain, routeBuilder.DefaultHandler, name, template, new RouteValueDictionary(defaults), new RouteValueDictionary(constraints), new RouteValueDictionary(dataTokens),
                new DefaultInlineConstraintResolver(routeOptions), routeOptions));

            return routeBuilder;
        }

        /// <summary>
        /// Adds a route to the IRouteBuilder with the specified hostnames, name and template.
        /// </summary>
        /// <param name="routeBuilder">The IRouteBuilder to add the route to.</param>
        /// <param name="hostnames">Hostnames which should be recognized by application.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="template">The URL pattern of the route.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IRouteBuilder MapRoute(this IRouteBuilder routeBuilder, string[] hostnames, string name, string template)
        {
            return MapRoute(routeBuilder, hostnames, name, template, defaults: null);
        }

        /// <summary>
        /// Adds a route to the IRouteBuilder with the specified hostnames, name and template.
        /// </summary>
        /// <param name="routeBuilder">The IRouteBuilder to add the route to.</param>
        /// <param name="hostnames">Hostnames which should be recognized by application.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="template">The URL pattern of the route.</param>
        /// <param name="defaults">An object that contains default values for route parameters. The object's properties represent the names and values of the default values.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IRouteBuilder MapRoute(this IRouteBuilder routeBuilder, string[] hostnames, string name, string template, object defaults)
        {
            return MapRoute(routeBuilder, hostnames, name, template, defaults, constraints: null);
        }

        /// <summary>
        /// Adds a route to the IRouteBuilder with the specified hostnames, name and template.
        /// </summary>
        /// <param name="routeBuilder">The IRouteBuilder to add the route to.</param>
        /// <param name="hostnames">Hostnames which should be recognized by application.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="template">The URL pattern of the route.</param>
        /// <param name="defaults">An object that contains default values for route parameters. The object's properties represent the names and values of the default values.</param>
        /// <param name="constraints">An object that contains constraints for the route. The object's properties represent the names and values of the constraints.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IRouteBuilder MapRoute(this IRouteBuilder routeBuilder, string[] hostnames, string name, string template, object defaults, object constraints)
        {
            return MapRoute(routeBuilder, hostnames, name, template, defaults, constraints, dataTokens: null);
        }

        /// <summary>
        /// Adds a route to the IRouteBuilder with the specified hostnames, name and template.
        /// </summary>
        /// <param name="routeBuilder">The IRouteBuilder to add the route to.</param>
        /// <param name="hostnames">Hostnames which should be recognized by application.</param>
        /// <param name="name">The name of the route.</param>
        /// <param name="template">The URL pattern of the route.</param>
        /// <param name="defaults">An object that contains default values for route parameters. The object's properties represent the names and values of the default values.</param>
        /// <param name="constraints">An object that contains constraints for the route. The object's properties represent the names and values of the constraints.</param>
        /// <param name="dataTokens">An object that contains data tokens for the route. The object's properties represent the names and values of the data tokens.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IRouteBuilder MapRoute(this IRouteBuilder routeBuilder, string[] hostnames, string name, string template, object defaults, object constraints, object dataTokens)
        {
            return MapSubdomainRoute(routeBuilder, hostnames, name, null, template, defaults, constraints, dataTokens);
        }
    }

}
