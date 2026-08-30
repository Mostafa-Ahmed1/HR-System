using Microsoft.AspNetCore.Mvc;
using HR_System.AttendanceImport;
using HR_System.Models;
using HR_System.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace HR_System.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private const int DisplayedImportErrorLimit = 25;
        private static readonly HashSet<string> AllowedImportExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".xls",
            ".xlsx"
        };

        private readonly HrSysContext db;
        private readonly IAttendanceImportService attendanceImportService;

        public AttendanceController(
            HrSysContext db,
            IAttendanceImportService attendanceImportService)
        {
            this.db = db;
            this.attendanceImportService = attendanceImportService;
        }
        [HrPermission(HrPage.Attendance, CrudOperation.Read)]
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

        [HrPermission(HrPage.Attendance, CrudOperation.Read)]
        public IActionResult list(string Search, int show)
        {
            var Gid = User.GetGroupId()?.ToString();
            if (Gid != null)
            {
                string pagename = "Attendance";
                ViewBag.groupId = db.CRUDs.FirstOrDefault(
                    n => n.GroupId == int.Parse(Gid) && n.Page.PageName == pagename);
            }
            if (String.IsNullOrEmpty(Search) && show != 0)
            {
                return PartialView(db.Att_dep.ToList().Take(show));
            }
            if (Search != null && show != 0)
            {
                var deps = db.Att_dep
                    .Where(n => n.Emp != null && n.Emp.EmpName != null && n.Emp.EmpName.Contains(Search))
                    .Take(show)
                    .ToList();
                return PartialView(deps);
            }
            return PartialView(db.Att_dep.ToList().Take(10));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HrPermission(HrPage.Attendance, CrudOperation.Delete)]
        public ActionResult Delete(int? id)
        {
            if (id != null)
            {
                AttDep? a = db.Att_dep.Where(n => n.AttId == id).FirstOrDefault();
                if (a != null)
                {
                    db.Att_dep.Remove(a);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                return NotFound();
            }
            return NotFound();

        }

        // GET: AttDeps/Create
        [HrPermission(HrPage.Attendance, CrudOperation.Add)]
        public IActionResult Create()
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
            ViewBag.EmpId = new SelectList(db.Employees, "EmpId", "EmpName");
            return View();
        }

        // POST: AttDeps/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [HrPermission(HrPage.Attendance, CrudOperation.Add)]
        public IActionResult Create([Bind("AttId,EmpId,Date,Attendance,Departure,EmpName")] AttDep attDep)
        {

            ViewBag.EmpId = new SelectList(db.Employees, "EmpId", "EmpName", attDep.EmpId);
            DateTime datebeforemonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month - 1, DateTime.Now.Day);
            if (attDep.Date > DateTime.Today || attDep.Date < datebeforemonth)
            {
                ViewBag.Date = "Sorry You can't add date in future Or month before";
                return View(attDep);
            }
            if (attDep.Departure < attDep.Attendance)
            {
                ViewBag.Departuretime = "Attendance must be before than Departure time";
                return View(attDep);
            }
            if (ModelState.IsValid)
            {
                db.Add(attDep);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(attDep);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(AttendanceImportDefaults.MaxUploadBytes)]
        [HrPermission(HrPage.Attendance, CrudOperation.Add)]
        public async Task<IActionResult> excelSubmit(
            AttendanceImportRequest request,
            CancellationToken cancellationToken)
        {
            var file = request.File;
            if (file is null || file.Length == 0 || file.Length > AttendanceImportDefaults.MaxUploadBytes)
            {
                return RenderImportFailure(new AttendanceImportError(
                    null,
                    "File",
                    "Select a non-empty attendance file no larger than 10 MB."));
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedImportExtensions.Contains(extension))
            {
                return RenderImportFailure(new AttendanceImportError(
                    null,
                    "File",
                    "Only .xls and .xlsx attendance files are accepted."));
            }

            await using var stream = file.OpenReadStream();
            var result = await attendanceImportService.ImportAsync(
                stream,
                file.FileName,
                cancellationToken);

            if (!result.Success)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                ViewBag.ImportErrorCount = result.TotalErrorCount;
                ViewBag.ImportErrors = result.Errors.Take(DisplayedImportErrorLimit).ToArray();
                return View("Index");
            }

            TempData["AttendanceImportSuccess"] =
                $"Attendance import completed successfully. Imported: {result.ImportedCount} rows.";
            return RedirectToAction("Index");
        }

        private IActionResult RenderImportFailure(params AttendanceImportError[] errors)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            ViewBag.ImportErrorCount = errors.Length;
            ViewBag.ImportErrors = errors.Take(DisplayedImportErrorLimit).ToArray();
            return View("Index");
        }
    }
}
