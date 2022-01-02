using HR_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Session;

namespace HR_System.Controllers
{
    public class operation : Controller
    {
        HrSysContext db;

        public operation(HrSysContext db)
        {
            this.db = db;

        }
        public IActionResult Index()
        {

            return View();
        }

        // GET: operation
        public IActionResult login()
        {
            //check if cookies file is exist or not
            if (Request.Cookies["id"] != null)
            {

                //Session.Add("userid", Request.Cookies["hrSystem"].Values["userid"]);

                if (Request.Cookies["role"] == "admin")
                {
                    var cookie = Request.Cookies["id"];
                    int id = int.Parse(cookie.ToString());
                    var admin = db.Admins.Find(id);


                    HttpContext.Session.SetString("adminId", admin.AdminId.ToString());
                    return RedirectToAction("Index", "Dashboard");


                }
                else if (Request.Cookies["role"] == "user")
                {
                    var cookie = Request.Cookies["id"];
                    int id = int.Parse(cookie.ToString());
                    var user = db.Users.Find(id);

                    int user_id = user.UserId;
                    HttpContext.Session.SetString("userId", user_id.ToString());

                    int group_id = (int)user.GroupId;

                    HttpContext.Session.SetString("groupId", group_id.ToString());
                    return RedirectToAction("Index", "Dashboard");
                }

            }
            return View();
        }
        [HttpPost]
        public ActionResult login(Admin a, bool remberme)
        {
            Admin admin = db.Admins.Where(n => n.AdminName == a.AdminName && n.AdminPass == a.AdminPass).FirstOrDefault();
            if (admin != null)
            {
                if (remberme == true)
                {
                    CookieOptions opt = new CookieOptions();
                    opt.Expires = DateTime.Now.AddDays(2);
                    Response.Cookies.Append("id", admin.AdminId.ToString(), opt);
                    Response.Cookies.Append("role", "admin", opt);
                }
                string id = admin.AdminId.ToString();
                HttpContext.Session.SetString("adminId", id);
                //HttpContext.Session.SetString("sessionSalary",JsonConvert.SerializeObject(sal));
                //var salarys =JsonConvert.DeserializeObject<SalaryVM>HttpContext.Session.GetString("sessionSalary");
                return RedirectToAction("Index", "Dashboard");
            }
            User user = db.Users.Where(n => n.Username == a.AdminName && n.Password == a.AdminPass).FirstOrDefault();
            if (user != null)
            {
                if (remberme == true)
                {
                    CookieOptions opt = new CookieOptions();
                    opt.Expires = DateTime.Now.AddDays(2);
                    Response.Cookies.Append("id", user.UserId.ToString(), opt);
                    Response.Cookies.Append("role", "user", opt);
                }
                int user_id = user.UserId;
                HttpContext.Session.SetString("userId", user_id.ToString());

                int group_id = (int)user.GroupId;
#pragma warning disable CS8604 // Possible null reference argument.
                HttpContext.Session.SetString("groupId", group_id.ToString());
#pragma warning restore CS8604 // Possible null reference argument.
                return RedirectToAction("Index", "Dashboard");
            }
            ViewBag.status = "incorrect email or password ";
            return View();
        }

        public ActionResult logout()
        {
            // delete the sessions
            var admin_id = HttpContext.Session.GetString("adminId");
            var user_id = HttpContext.Session.GetString("userId");

            if (admin_id != null)
            {
                HttpContext.Session.Remove("adminId");
            }
            else
            {
                HttpContext.Session.Remove("userId");
                HttpContext.Session.Remove("groupId");
            }

            //Erase the data in the cookie
            CookieOptions option = new CookieOptions();
            option.Expires = DateTime.Now.AddDays(-1);
            option.Secure = true;
            option.IsEssential = true;
            Response.Cookies.Append("id", string.Empty, option);
            Response.Cookies.Append("role", string.Empty, option);
            //Then delete the cookie
            Response.Cookies.Delete("id");
            Response.Cookies.Delete("role");

            return RedirectToAction("login");

        }
    }
}

