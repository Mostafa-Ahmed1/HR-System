using Microsoft.AspNetCore.Mvc;
using HR_System.Models;
using HR_System.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HR_System.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        HrSysContext db;

        public SettingsController(HrSysContext db)
        {
            this.db = db;
        }
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
            var gId = User.GetGroupId()?.ToString();
            string pageName = "General Settings";
            if (gId != null)
            {
                ViewBag.groupId = db.CRUDs.Where(n => n.GroupId == int.Parse(gId) && n.Page.PageName == pageName).FirstOrDefault();
            }
            var setts = db.Settings.FirstOrDefault();
            ViewBag.vac = new SelectList(new List<string>() { "Saturday", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" });
            if (setts == null)
            {
                Setting s = new Setting()
                {
                    PlusPerhour = 0,
                    MinusPerhour = 0,
                    Dayoff1 = "",
                    Dayoff2 = ""

                };
                return View(s);
            }
            else
            {
                return View(setts);
            }

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(Setting s)
        {

            var sett = db.Settings.ToList().Count;
            if (ModelState.IsValid && sett == 0)

            {
                db.Settings.Add(s);
                db.SaveChanges();
                return RedirectToAction("index");
            }

            else if (ModelState.IsValid && sett > 0)
            {

                Setting se = db.Settings.Find(s.SettingId);
                if (se != null)
                {

                    se.PlusPerhour = s.PlusPerhour;
                    se.MinusPerhour = s.MinusPerhour;
                    se.Dayoff1 = s.Dayoff1;
                    se.Dayoff2 = s.Dayoff2;
                    db.SaveChanges();
                    return RedirectToAction("index");
                }
                else
                {
                    return NotFound();
                }


            }
            else
            {
                return View(s);
            }
        }

    }
}
