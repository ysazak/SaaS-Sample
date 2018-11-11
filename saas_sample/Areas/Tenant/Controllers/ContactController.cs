using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using saas_sample.Data;
using saas_sample.DataModels;

namespace saas_sample.Areas.Tenant.Controllers
{
    [Area("Tenant")]
    public class ContactController : BaseController
    {


        public ContactController()
        {

        }
        public IActionResult Index()
        {
            using (var dbcontext = CreateContext())
            {
                //dbcontext.Contacts.Add(new Contact { Name = "yasar1" });
                //dbcontext.SaveChanges();
                return View(dbcontext.Contacts.ToList());
            }
        }

        [HttpGet]
        public IActionResult Create(Contact contact)
        {
            return View();
        }


        [HttpPost]
        public IActionResult CreateContact(Contact contact)
        {
            if (!ModelState.IsValid)
                return View(contact);

            using (var dbcontext = CreateContext())
            {
                dbcontext.Contacts.Add(contact);
                dbcontext.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}