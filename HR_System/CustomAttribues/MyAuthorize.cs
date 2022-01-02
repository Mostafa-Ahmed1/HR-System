using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HR_System.Models;
using static Microsoft.AspNetCore.Http.HttpContextAccessor;

namespace HR_System.CustomAttribues
{
    public class inheritController : Controller
    {
        private readonly HrSysContext _context;

        public inheritController(HrSysContext context)
        {
            _context = context;

        }


        public bool checkAuth(string? admin_id,string? user_id, string? group_id)
        {
            //string? admin_id = HttpContext.Session.GetString("adminId");
            //var user_id = HttpContext.Session.GetString("userId");
            //var group_id = HttpContext.Session.GetString("groupId");
            if (user_id != null)
            {
                if (group_id != null)
                {
                    List<Crud> Rules = _context.CRUDs.Where(n => n.GroupId == int.Parse(group_id.ToString())).ToList();
                    ViewBag.PagesRules = Rules;
                    return true;
                }
            }
            if (admin_id != null)
            {
                ViewBag.PagesRules = null;
                return true;
            }
            return false;
        }
    }
    public static class MyAuthorize
    {
        public static bool gotoaction(string? admin_id, string? user_id, string? group_id)
        {
            HrSysContext db = new HrSysContext();
            inheritController ic = new inheritController(db);
            return ic.checkAuth(admin_id,user_id,group_id);

        }
    }
}
