using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using saas_sample.Data;
using saas_sample.DataModels;

namespace saas_sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();
                
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    SetupDbs(services);

                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database.");
                }
            }

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();


        private static void SetupDbs(IServiceProvider serviceProvider)
        {

            using (var db = serviceProvider.GetService<AppDbContext>())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
                db.Tenants.Add(new Tenant { Name = "Tenant1" });
                db.Tenants.Add(new Tenant { Name = "Tenant2" });
                db.SaveChanges();
            }

            //using (var serviceScope = serviceProvider.CreateScope())
            //{

            //    using (var db = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>())
            //    {
            //        db.Database.EnsureDeleted();
            //        db.Database.EnsureCreated();
            //        db.Tenants.Add(new Tenant {Name = "Tenant1"});
            //        db.Tenants.Add(new Tenant {Name = "Tenant2"});
            //        db.SaveChanges();
            //    }
            //}
        }
    }
}
