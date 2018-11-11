using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using saas_sample.Data;
using saas_sample.DataModels;
using saas_sample.Extensions;

namespace saas_sample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddSingleton<CustomRouter>();
            //services.AddSingleton<ITenantStore, TenantStore>();
            //services.AddSubdomains();


            var configurationOptions = Configuration.GetSection("Saas:Configurations").Get<ConfigurationOptions>();
            string connStr = string.Format(configurationOptions.DefaultConnectionString,
                configurationOptions.Catalog.Identifier);

            services.AddEntityFrameworkSqlServer()
                .AddDbContext<AppDbContext>(options => { options.UseSqlServer(connStr); });

            //services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connStr));

            //In Memory DB
            //services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("test"));

            //services.AddDbContext<TenantDbContext>(opt => opt.UseSqlServer()());

            //services.TryAddSingleton<IHttpContextAccessor, TenantHttpContextAccessor>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, CustomRouter customRouter)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseTenantSiteResolver();

            app.UseMvc(routes =>
            {
                routes.DefaultHandler = customRouter;
                routes.MapRoute(name: "areaRoute",
                    template: "{controller=Home}/{action=Index}");
            });

        }
       
    }


}
