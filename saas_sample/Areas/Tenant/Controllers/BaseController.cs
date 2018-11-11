using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using saas_sample.Data;
using saas_sample.Extensions;
using saas_sample.Models;

namespace saas_sample.Areas.Tenant.Controllers
{
    public class BaseController : Controller
    {
        protected TenantContext TenantHttpContext { get; private set; }
        protected TenantSiteFeature TenantSiteFeature { get; private set; }

        public BaseController()
        {

        }


        public override void OnActionExecuting(ActionExecutingContext ctx)
        {
            base.OnActionExecuting(ctx);
            TenantHttpContext = HttpContext.GetTenantContext();
            TenantSiteFeature = HttpContext.GetTenantSiteFeature();
        }

        protected TenantDbContext CreateContext()
        {

            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            var options = optionsBuilder.UseSqlServer(TenantHttpContext?.TenantInfo?.ConnectionString).Options;

            var dbContext = new TenantDbContext(TenantHttpContext?.TenantInfo, options);
            dbContext.Database.EnsureCreated();
            return dbContext;
        }
    }
}