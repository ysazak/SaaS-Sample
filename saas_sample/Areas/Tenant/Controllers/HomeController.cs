using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace saas_sample.Areas.Tenant.Controllers
{
    [Area("Tenant")]
    public class HomeController :  BaseController
    {
        public IActionResult Index()
        {            
            return View(TenantHttpContext.TenantInfo);
        }
    }
}