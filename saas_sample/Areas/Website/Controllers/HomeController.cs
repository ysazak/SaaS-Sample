using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using saas_sample.Data;
using saas_sample.Models;

namespace saas_sample.Areas.Website.Controllers
{
    [Area("Website")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _appDbContext;

        public HomeController(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        public IActionResult Index()
        {
            var result = _appDbContext.Tenants.Select(t => new TenantViewModel
            {
                Id = t.Id,
                Name = t.Name,
                SiteUrl = $"{t.Name}.localhost:9863"
            });
            return View(result);
        }

        [HttpPost]
        public IActionResult AddTenant(string name)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestResult();
            }

            var newTenant = new DataModels.Tenant{Name= name};
            _appDbContext.Tenants.Add(newTenant);
            _appDbContext.SaveChanges();
            return RedirectToAction("Index");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}