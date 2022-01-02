using HR_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Controllers;
public class UserController : Controller
{
    HrSysContext db;
    public string pagename { get; set; }
    public UserController(HrSysContext db)
    {
        this.db = db;
        pagename = "Users";
    }

    // List Users
    public IActionResult Index()
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
                List<Crud> Rules = db.CRUDs.Where(n => n.GroupId == int.Parse(group_id)).ToList();
                ViewBag.PagesRules = Rules;
            }
        }
        var gId = HttpContext.Session.GetString("groupId");
        if (gId != null)
        {
            string pagename = "Users";
            Crud crud = db.CRUDs.Where(n => n.GroupId == int.Parse(gId.ToString()) && n.Page.PageName == pagename).FirstOrDefault();
            ViewBag.groupId = crud;
            if (!crud.Read) return RedirectToAction("HttpStatusCodeHandler", "error", new { StatusCode = 401 });
        }

        return View(db.Users.ToList());
    }

    public IActionResult allusers(string search, int show)
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
                List<Crud> Rules = db.CRUDs.Where(n => n.GroupId == int.Parse(group_id)).ToList();
                ViewBag.PagesRules = Rules;
                Crud crud = db.CRUDs.Where(n => n.GroupId == int.Parse(group_id.ToString()) && n.Page.PageName == pagename).FirstOrDefault();
                ViewBag.groupId = crud;
                if (!crud.Read) return RedirectToAction("HttpStatusCodeHandler", "error", new { StatusCode = 401 });
            }
        }
        var users = db.Users.Include(e => e.Group).ToList();
        if (search != null && show != 0)
        {
            var usersfiltered = users.Where(e => e.Username.Contains(search)).Take(show);
            return PartialView(usersfiltered);
        }
        if (search != null)
        {
            var usersfiltered = db.Users.Include(e => e.Group).Where(e => e.Username.Contains(search));
            return PartialView(usersfiltered);
        }
        if (show != 0)
        {
            return PartialView(users.Take(show));
        }
        return PartialView(users.Take(10));    
    }
    public IActionResult addUser()
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
                List<Crud> Rules = db.CRUDs.Where(n => n.GroupId == int.Parse(group_id)).ToList();
                ViewBag.PagesRules = Rules;
            }
        }
        var gId = HttpContext.Session.GetString("groupId");
        if (gId != null)
        {
            string pagename = "Users";
            Crud crud = db.CRUDs.Where(n => n.GroupId == int.Parse(gId.ToString()) && n.Page.PageName == pagename).FirstOrDefault();
            ViewBag.groupId = crud;
            if (!crud.Add) return RedirectToAction("HttpStatusCodeHandler", "error", new { StatusCode = 401 });
        }
        // Send Groups Drop Down List Data 
        ViewBag.groups = new SelectList( db.Groups.ToList() , "GroupId", "GroupName");
        return View();
    }

    [HttpPost]
    public IActionResult addUser(User newUser)
    {
        db.Users.Add(newUser);
        db.SaveChanges();
        return RedirectToAction( "Index","User");
    }

    
    // Edit User
    public IActionResult edit(int id)
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
                List<Crud> Rules = db.CRUDs.Where(n => n.GroupId == int.Parse(group_id)).ToList();
                ViewBag.PagesRules = Rules;
                string pagename = "Users";
                Crud crud = db.CRUDs.Where(n => n.GroupId == int.Parse(group_id.ToString()) && n.Page.PageName == pagename).FirstOrDefault();
                ViewBag.groupId = crud;
                if (!crud.Add) return RedirectToAction("HttpStatusCodeHandler", "error", new { StatusCode = 401 });
            }
        }
        User OldUser =db.Users.Find(id);
        ViewBag.groups = new SelectList(db.Groups.ToList(), "GroupId", "GroupName");

        return View(OldUser);
    }

    [HttpPost]
    public IActionResult edit(User newUser)
    {
        User old = db.Users.Find(newUser.UserId);
        old.Username = newUser.Username;
        old.Email = newUser.Email;
        old.GroupId = newUser.GroupId;
        db.SaveChanges();

        return RedirectToAction("Index", "User");
    }
    // Delete User
    public IActionResult delete(int? id)
    {
        var x = db.Users.Find(id);
        if (x != null)
        {
            db.Users.Remove(x);
            db.SaveChanges();
        }
        else
        {
            return NotFound(); 
        }
        return RedirectToAction("Index", "User");
    }
}
