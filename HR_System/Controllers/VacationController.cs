using Microsoft.AspNetCore.Mvc;
using HR_System.Models;

namespace HR_System.Controllers
{
    public class VacationController : Controller
    {
        HrSysContext db;
        public VacationController(HrSysContext db)
        {
            this.db = db;
        }
        public IActionResult Index()
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
            return View();
        }

        [HttpPost]
	[ValidateAntiForgeryToken]
        public IActionResult Index(Vacation v)
        {
            if (ModelState.IsValid)
            {
                db.Vacations.Add(v);
                db.SaveChanges();
                return RedirectToAction("display");
            }
            else
            {
                return View(v);

            }

        }
        public IActionResult display()
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
            return View(db.Vacations.ToList());
        }

    }
}
