using HR_System.Models;
using HR_System.Security;
using HR_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Controllers;
[Authorize]
public class UserController : Controller
{
    private readonly HrSysContext db;
    private readonly IPasswordMigrationService<User> _passwords;
    public string pagename { get; set; }
    public UserController(HrSysContext db, IPasswordMigrationService<User> passwords)
    {
        this.db = db;
        _passwords = passwords;
        pagename = "Users";
    }

    // List Users
    [HrPermission(HrPage.Users, CrudOperation.Read)]
    public IActionResult Index()
    {
        var admin_id = User.GetAdminId()?.ToString();
        var user_id = User.GetUserId()?.ToString();
        var group_id = User.GetGroupId()?.ToString();
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
        var gId = User.GetGroupId()?.ToString();
        if (gId != null)
        {
            string pagename = "Users";
            Crud? crud = db.CRUDs.FirstOrDefault(n => n.GroupId == int.Parse(gId) && n.Page.PageName == pagename);
            ViewBag.groupId = crud;
        }

        return View(db.Users.ToList());
    }

    [HrPermission(HrPage.Users, CrudOperation.Read)]
    public IActionResult allusers(string search, int show)
    {
        var admin_id = User.GetAdminId()?.ToString();
        var user_id = User.GetUserId()?.ToString();
        var group_id = User.GetGroupId()?.ToString();
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
                Crud? crud = db.CRUDs.FirstOrDefault(n => n.GroupId == int.Parse(group_id) && n.Page.PageName == pagename);
                ViewBag.groupId = crud;
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
    [HrPermission(HrPage.Users, CrudOperation.Add)]
    public IActionResult addUser()
    {
        var admin_id = User.GetAdminId()?.ToString();
        var user_id = User.GetUserId()?.ToString();
        var group_id = User.GetGroupId()?.ToString();
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
        var gId = User.GetGroupId()?.ToString();
        if (gId != null)
        {
            string pagename = "Users";
            Crud? crud = db.CRUDs.FirstOrDefault(n => n.GroupId == int.Parse(gId) && n.Page.PageName == pagename);
            ViewBag.groupId = crud;
        }
        // Send Groups Drop Down List Data 
        ViewBag.groups = new SelectList( db.Groups.ToList() , "GroupId", "GroupName");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HrPermission(HrPage.Users, CrudOperation.Add)]
    public IActionResult addUser(User newUser)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.groups = new SelectList(db.Groups.ToList(), "GroupId", "GroupName", newUser.GroupId);
            return View(newUser);
        }

        newUser.Password = _passwords.Hash(newUser, newUser.Password);
        db.Users.Add(newUser);
        db.SaveChanges();
        return RedirectToAction( "Index","User");
    }

    
    // Edit User
    [HrPermission(HrPage.Users, CrudOperation.Update)]
    public IActionResult edit(int id)
    {
        var admin_id = User.GetAdminId()?.ToString();
        var user_id = User.GetUserId()?.ToString();
        var group_id = User.GetGroupId()?.ToString();
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
                Crud? crud = db.CRUDs.FirstOrDefault(n => n.GroupId == int.Parse(group_id) && n.Page.PageName == pagename);
                ViewBag.groupId = crud;
            }
        }
        User? oldUser = db.Users.Find(id);
        if (oldUser is null)
        {
            return NotFound();
        }

        ViewBag.groups = new SelectList(db.Groups.ToList(), "GroupId", "GroupName");

        return View(new EditUserViewModel
        {
            UserId = oldUser.UserId,
            Username = oldUser.Username,
            Email = oldUser.Email,
            GroupId = oldUser.GroupId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HrPermission(HrPage.Users, CrudOperation.Update)]
    public IActionResult edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.groups = new SelectList(db.Groups.ToList(), "GroupId", "GroupName", model.GroupId);
            return View(model);
        }

        User? old = db.Users.Find(model.UserId);
        if (old is null)
        {
            return NotFound();
        }

        old.Username = model.Username;
        old.Email = model.Email;
        old.GroupId = model.GroupId;
        db.SaveChanges();

        return RedirectToAction("Index", "User");
    }
    // Delete User
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HrPermission(HrPage.Users, CrudOperation.Delete)]
    public IActionResult delete(int id)
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
