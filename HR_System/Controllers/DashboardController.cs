using HR_System.Models;
using HR_System.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace HR_System.Controllers
{
    [Authorize]
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
            var admin_id = User.GetAdminId()?.ToString();
            var user_id = User.GetUserId()?.ToString();

            if (admin_id != null)
            {
                ViewBag.PagesRules = null;
            }
            else if (user_id != null)
            {
                var b = User.GetGroupId()?.ToString();
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
