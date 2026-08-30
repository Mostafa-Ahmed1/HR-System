using Microsoft.AspNetCore.Mvc;
using HR_System.Models;
using HR_System.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        HrSysContext db;
        private readonly IHrPermissionService _permissions;

        public SettingsController(HrSysContext db, IHrPermissionService permissions)
        {
            this.db = db;
            _permissions = permissions;
        }
        [HrPermission(HrPage.GeneralSettings, CrudOperation.Read)]
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
        public async Task<IActionResult> Index(Setting s, CancellationToken cancellationToken)
        {
            var sett = await db.Settings.AsNoTracking().CountAsync(cancellationToken);
            var operation = sett == 0 ? CrudOperation.Add : CrudOperation.Update;
            if (!await _permissions.HasPermissionAsync(
                    User,
                    HrPage.GeneralSettings,
                    operation,
                    cancellationToken))
            {
                return Forbid();
            }

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
