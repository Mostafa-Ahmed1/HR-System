using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HR_System.Models;
using Microsoft.AspNetCore.Mvc;
using HR_System.Models;
using Newtonsoft.Json;


namespace HR_System.Controllers
{
    public class DashboardController : Controller
    {
        private readonly HrSysContext db;

        public DashboardController(HrSysContext db)
        {
            this.db = db;
        }

        // GET: Dashboard
        public async Task<IActionResult> Index()
        {
            var admin_id = HttpContext.Session.GetString("adminId");
            var user_id = HttpContext.Session.GetString("userId");

            if (admin_id != null)
            {
                ViewBag.PagesRules = null;
            }
            else if (user_id != null)
            {
                var b = HttpContext.Session.GetString("groupId");
                if (b != null)
                {
                    List<Crud> Rules = db.CRUDs.Where(n => n.GroupId == int.Parse(b)).ToList();
                    ViewBag.PagesRules = Rules;

                }
            }
            var hrSysContext = db.Att_dep.Include(a => a.Emp);
            return View(await hrSysContext.ToListAsync());
        }
    }
}
