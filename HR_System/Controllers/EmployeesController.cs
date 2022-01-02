using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HR_System.Models;
using Microsoft.AspNetCore.Authorization;
using HR_System.CustomAttribues;

namespace HR_System.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly HrSysContext _context;
        public string pagename { get; set; }
        public EmployeesController(HrSysContext context)
        {
            pagename = "Employees";
            _context = context;
        }

        // GET: Employees
        public IActionResult Index()
        {
            var admin_id = HttpContext.Session.GetString("adminId");
            var user_id = HttpContext.Session.GetString("userId");
            var group_id = HttpContext.Session.GetString("groupId");
            if(admin_id == null && user_id == null)
            {
                return RedirectToAction("login","operation");
            }
            if (admin_id != null)
            {
                ViewBag.PagesRules = null;
            }
            else if (user_id != null)
            {
                if (group_id != null)
                {
                    List<Crud> Rules = _context.CRUDs.Where(n => n.GroupId == int.Parse(group_id)).ToList();
                    ViewBag.PagesRules = Rules;
                    Crud crud = _context.CRUDs.Where(n => n.GroupId == int.Parse(group_id.ToString()) && n.Page.PageName == pagename).FirstOrDefault();
                    ViewBag.groupId = crud;
                    if (!crud.Read) return RedirectToAction("HttpStatusCodeHandler", "error", new { StatusCode = 401 });
                }
            }
            return View();
        }
        // GET: AllEmployees 
        public IActionResult allEmployees(string search, int show)
        {
            var admin_id = HttpContext.Session.GetString("adminId");
            var user_id = HttpContext.Session.GetString("userId");
            var group_id = HttpContext.Session.GetString("groupId");
            if (admin_id == null && user_id == null)
            {
                return RedirectToAction("login", "operation");
            }
            if (admin_id != null)
            {
                ViewBag.PagesRules = null;
            }
            else if (user_id != null)
            {
                if (group_id != null)
                {
                    List<Crud> Rules = _context.CRUDs.Where(n => n.GroupId == int.Parse(group_id)).ToList();
                    ViewBag.PagesRules = Rules;
                    Crud crud = _context.CRUDs.Where(n => n.GroupId == int.Parse(group_id.ToString()) && n.Page.PageName == pagename).FirstOrDefault();
                    ViewBag.groupId = crud;
                    if (!crud.Read) return RedirectToAction("HttpStatusCodeHandler", "error", new { StatusCode = 401 });
                }
            }
            var employees = _context.Employees.Include(e => e.Dept).ToList();
            if (search != null && show != 0)
            {
                var emps = employees.Where(e => e.EmpName.Contains(search)).Take(show);
                return PartialView(emps);
            }
            if (search != null)
            {
                var emps = _context.Employees.Include(e => e.Dept).Where(e => e.EmpName.Contains(search));
                return PartialView(emps);
            }
            if (show != 0)
            {
                return PartialView(employees.Take(show));
            }
            return PartialView(employees.Take(10));
        }
        // GET: Employees/Details/5
        public IActionResult Details(int? id)
        {
            var admin_id = HttpContext.Session.GetString("adminId");
            var user_id = HttpContext.Session.GetString("userId");
            var group_id = HttpContext.Session.GetString("groupId");
            if (admin_id == null && user_id == null)
            {
                return RedirectToAction("login", "operation");
            }
            if (admin_id != null)
            {
                ViewBag.PagesRules = null;
            }
            else if (user_id != null)
            {
                if (group_id != null)
                {
                    List<Crud> Rules = _context.CRUDs.Where(n => n.GroupId == int.Parse(group_id)).ToList();
                    ViewBag.PagesRules = Rules;
                    Crud crud = _context.CRUDs.Where(n => n.GroupId == int.Parse(group_id.ToString()) && n.Page.PageName == pagename).FirstOrDefault();
                    ViewBag.groupId = crud;
                    if (!crud.Read) return RedirectToAction("HttpStatusCodeHandler", "error", new { StatusCode = 401 });
                }
            }           
            if (id == null)
            {
                return NotFound();
            }
            var employee = _context.Employees
                .Include(e => e.Dept)
                .FirstOrDefault(m => m.EmpId == id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }
        // Remote Validations for Employee BirthDate...
        public IActionResult birthdatecheck(DateTime Birthdate)
        {
            DateTime datebefore20 = new DateTime(DateTime.Now.Year-20,DateTime.Now.Month,DateTime.Now.Day);
            
            if (Birthdate < datebefore20) return Json(true);
            else return Json(false);
        }
        // Remote Validations for Employee HireDate...
        public JsonResult hiredatecheck(DateTime Hiredate)
        {
            DateTime companystartdate = new DateTime(2008,1,1);

            if (Hiredate > companystartdate) return Json(true);
            else return Json(false);
        }
        public IActionResult Create()
        {
            var admin_id = HttpContext.Session.GetString("adminId");
            var user_id = HttpContext.Session.GetString("userId");
            var group_id = HttpContext.Session.GetString("groupId");
            if (admin_id == null && user_id == null)
            {
                return RedirectToAction("login", "operation");
            }
            if (admin_id != null)
            {
                ViewBag.PagesRules = null;
            }
            else if (user_id != null)
            {
                if (group_id != null)
                {
                    List<Crud> Rules = _context.CRUDs.Where(n => n.GroupId == int.Parse(group_id)).ToList();
                    ViewBag.PagesRules = Rules;
                    Crud crud = _context.CRUDs.Where(n => n.GroupId == int.Parse(group_id.ToString()) && n.Page.PageName == pagename).FirstOrDefault();
                    ViewBag.groupId = crud;
                    if (!crud.Add) return RedirectToAction("HttpStatusCodeHandler", "error", new { StatusCode = 401 });
                }
            }
            ViewBag.Gender = new SelectList(new List<string>() { "Male", "Female" });
            ViewBag.Depts = new SelectList(_context.Departments, "DeptId", "DeptName");
            return View();
        }

        // POST: Employees/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Employee employee)
        {
            if (employee.DepartureTime < employee.AttTime)
            {
                ViewBag.deptafteratt = "Attendance Time Can't Be After Departure Time!";
                return View(employee);
            }
            if (ModelState.IsValid)
            {
                _context.Add(employee);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Gender = new SelectList(new List<string>() { "Male", "Female" }, employee.Gender);
            ViewBag.Depts = new SelectList(_context.Departments, "DeptId", "DeptName", employee.DeptId);
            return View(employee);
        }
        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            var admin_id = HttpContext.Session.GetString("adminId");
            var user_id = HttpContext.Session.GetString("userId");
            var group_id = HttpContext.Session.GetString("groupId");
            if (admin_id == null && user_id == null)
            {
                return RedirectToAction("login", "operation");
            }
            if (admin_id != null)
            {
                ViewBag.PagesRules = null;
            }
            else if (user_id != null)
            {
                if (group_id != null)
                {
                    List<Crud> Rules = _context.CRUDs.Where(n => n.GroupId == int.Parse(group_id)).ToList();
                    ViewBag.PagesRules = Rules;
                    Crud crud = _context.CRUDs.Where(n => n.GroupId == int.Parse(group_id.ToString()) && n.Page.PageName == pagename).FirstOrDefault();
                    ViewBag.groupId = crud;
                    if (!crud.Update) return RedirectToAction("HttpStatusCodeHandler", "error", new { StatusCode = 401 });
                }
            }
            if (id == null)
            {
                return NotFound();
            }
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            ViewBag.Gender = new SelectList(new List<string>() { "Male", "Female" }, employee.Gender);
            ViewBag.Depts = new SelectList(_context.Departments, "DeptId", "DeptName", employee.DeptId);
            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee)
        {
            if (id != employee.EmpId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.EmpId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Gender = new SelectList(new List<string>() { "Male", "Female" }, employee.Gender);
            ViewBag.Depts = new SelectList(_context.Departments, "DeptId", "DeptId", employee.DeptId);
            return View(employee);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            var admin_id = HttpContext.Session.GetString("adminId");
            var user_id = HttpContext.Session.GetString("userId");
            var group_id = HttpContext.Session.GetString("groupId");
            if (admin_id == null && user_id == null)
            {
                return RedirectToAction("login", "operation");
            }
            if (admin_id != null)
            {
                ViewBag.PagesRules = null;
            }
            else if (user_id != null)
            {
                if (group_id != null)
                {
                    List<Crud> Rules = _context.CRUDs.Where(n => n.GroupId == int.Parse(group_id)).ToList();
                    ViewBag.PagesRules = Rules;
                    Crud crud = _context.CRUDs.Where(n => n.GroupId == int.Parse(group_id.ToString()) && n.Page.PageName == pagename).FirstOrDefault();
                    ViewBag.groupId = crud;
                    if (!crud.Delete) return RedirectToAction("HttpStatusCodeHandler", "error", new { StatusCode = 401 });
                }
            }
            if (id == null)
            {
                return NotFound();
            }
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();


            return RedirectToAction(nameof(Index));
        }

        //// POST: Employees/Delete/5
        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.EmpId == id);
        }
    }
}
