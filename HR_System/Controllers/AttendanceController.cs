using Microsoft.AspNetCore.Mvc;
using HR_System.Models;
using HR_System.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Text;
using ExcelDataReader;


namespace HR_System.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private const long MaxAttendanceImportSize = 10 * 1024 * 1024;
        private static readonly HashSet<string> AllowedImportExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".xls",
            ".xlsx"
        };

        private HrSysContext db;
        public AttendanceController(HrSysContext db)
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
            return View();
        }

        public IActionResult list(string Search, int show)
        {
            var Gid = User.GetGroupId()?.ToString();
            if (Gid != null)
            {
                string pagename = "Attendance";
                ViewBag.groupId = db.CRUDs.Where(n => n.GroupId == int.Parse(Gid) && n.PageId == int.Parse(pagename));
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
        [RequestSizeLimit(MaxAttendanceImportSize)]
        public IActionResult excelSubmit(IFormFile? file)
        {
            if (file is null || file.Length == 0 || file.Length > MaxAttendanceImportSize)
            {
                return BadRequest("Select a non-empty attendance file no larger than 10 MB.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedImportExtensions.Contains(extension))
            {
                return BadRequest("Only .xls and .xlsx attendance files are accepted.");
            }

            using var stream = file.OpenReadStream();
            GetAttendenceList(stream);

            return RedirectToAction("Index");

        }

        private void GetAttendenceList(Stream stream)
        {
            List<AttDep> records = new List<AttDep>();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                while (reader.Read())
                {
                    double code = (double)reader.GetValue(0);
                    DateTime att = (DateTime)reader.GetValue(2);
                    DateTime dep = (DateTime)reader.GetValue(3);


                    records.Add(new AttDep()
                    {

                        EmpId = (int)code,
                        Date = (DateTime)reader.GetValue(1),
                        Attendance = att.TimeOfDay,
                        Departure = dep.TimeOfDay
                    });
                }

                foreach (var record in records)
                {

                    db.Att_dep.Add(record);
                }
                db.SaveChanges();

            }
        }




    }
}
