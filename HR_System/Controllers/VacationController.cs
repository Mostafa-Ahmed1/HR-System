using Microsoft.AspNetCore.Mvc;
using HR_System.Models;
using HR_System.Security;
using Microsoft.AspNetCore.Authorization;

namespace HR_System.Controllers
{
    [Authorize]
    public class VacationController : Controller
    {
        HrSysContext db;
        public VacationController(HrSysContext db)
        {
            this.db = db;
        }
        [HrPermission(HrPage.Vacations, CrudOperation.Add)]
        public IActionResult Index()
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
            return View();
        }

        [HttpPost]
	[ValidateAntiForgeryToken]
        [HrPermission(HrPage.Vacations, CrudOperation.Add)]
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
        [HrPermission(HrPage.Vacations, CrudOperation.Read)]
        public IActionResult display()
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
            return View(db.Vacations.ToList());
        }

    }
}
