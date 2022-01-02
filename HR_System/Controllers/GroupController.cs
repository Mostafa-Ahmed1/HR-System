using Microsoft.AspNetCore.Mvc;
using HR_System.Models;
using Microsoft.EntityFrameworkCore;

namespace HR_System.Controllers
{
    // ModelState.Remove("group");
    public class GroupController : Controller
    {
        HrSysContext db;
        public GroupController(HrSysContext db)
        {
            this.db = db;
        }
        public IActionResult Index()
        {
            var admin_id = HttpContext.Session.GetString("adminId");
            var user_id = HttpContext.Session.GetString("userId");

            if (admin_id != null)
            {
                ViewBag.PagesRules = null;
            }
            else if (user_id != null)
            {
                var b = HttpContext.Session.GetString("groupId");
                if (b != null)
                {
                    List<Crud> Rules = db.CRUDs.Where(n => n.GroupId == int.Parse(b)).ToList();
                    ViewBag.PagesRules = Rules;

                }
            }
            var gId = HttpContext.Session.GetString("groupId");
            string pageName = "Permissions";
            if (gId != null)
            {
                ViewBag.groupId = db.CRUDs.Where(n => n.GroupId == int.Parse(gId) && n.Page.PageName == pageName).FirstOrDefault();
            }

            List<Group> groups = db.Groups.ToList();
            return View(groups);
        }

        //search and show
        public IActionResult groupSearch(string search,int show)
        {
            var gId = HttpContext.Session.GetString("groupId");
            string pageName = "Permissions";
            if (gId != null)
            {
                ViewBag.groupId = db.CRUDs.Where(n => n.GroupId == int.Parse(gId) && n.Page.PageName == pageName).FirstOrDefault();
            }

            List <Group> allgroups = db.Groups.OrderBy(n=>n.GroupId).ToList();

            if (!string.IsNullOrEmpty(search) && show != 0)
            {
                List<Group> searchShow = db.Groups.Where(n => n.GroupName.Contains(search)).Take(show).ToList();
                return PartialView(searchShow);
            }

            if (!string.IsNullOrEmpty(search))
            {
                List<Group> searchG = db.Groups.Where(n=>n.GroupName.Contains(search)).ToList();
                return PartialView(searchG);
                
            }
            
            if (show!=0)
            {
                List<Group> showG = db.Groups.Take(show).ToList();
                return PartialView(showG);
            }
            return PartialView(allgroups);
        }
        //create
        public IActionResult CreateGroup()
        {
            var admin_id = HttpContext.Session.GetString("adminId");
            var user_id = HttpContext.Session.GetString("userId");

            if (admin_id != null)
            {
                ViewBag.PagesRules = null;
            }
            else if (user_id != null)
            {
                var b = HttpContext.Session.GetString("groupId");
                if (b != null)
                {
                    List<Crud> Rules = db.CRUDs.Where(n => n.GroupId == int.Parse(b)).ToList();
                    ViewBag.PagesRules = Rules;

                }
            }
            List <PageCrud> pc=new List<PageCrud>();
            foreach(var item in db.Pages.ToList())
            {
                pc.Add(new PageCrud { page = item });
            }
            GroupRelation groupRelation = new GroupRelation()
            {
                group=new Group(),
                pageCruds=pc
            };
            ViewBag.PageCrud = groupRelation.pageCruds.Count;
            return View(groupRelation);
        }
        //create action
        [HttpPost]
        public IActionResult CreateGroup(GroupRelation g)
        {
            if (ModelState.IsValid)
            {
               
                //solve duplicated group name
                List<Group> nameValid = db.Groups.ToList();
                foreach(var item in nameValid)
                {
                    if (g.group.GroupName == item.GroupName)
                    {
                        ViewBag.NameValidation = "*This Group Name Is Already Exist";

                        List<PageCrud> pcEroor = new List<PageCrud>();
                        foreach (var itemV in db.Pages.ToList())
                        {
                            pcEroor.Add(new PageCrud { page = itemV });
                        }
                        GroupRelation groupRelationError = new GroupRelation()
                        {
                            group = new Group(),
                            pageCruds = pcEroor
                        };
                        ViewBag.PageCrud = groupRelationError.pageCruds.Count;
                        return View(groupRelationError);
                    }
                    else
                    {
                        ViewBag.NameValidation = "";                       
                    }
                    
                }
                //create group name if not exist in db
                Group group = new Group();
                group = g.group;
                db.Groups.Add(group);
                db.SaveChanges();
                int count = 0;
                
                foreach (var item in g.pageCruds)
                {
                    Crud crud = new Crud();
                    crud.GroupId = g.group.GroupId;
                    crud.PageId = item.page.PageId;
                    if (item.ADD)
                    {
                        crud.Add = true;
                    }
                    else
                    {
                        crud.Add = false;
                    }
                    if (item.Read)
                    {
                        crud.Read = true;
                    }
                    else
                    {
                        crud.Read = false;
                    }
                    if (item.Update)
                    {
                        crud.Update = true;
                    }
                    else
                    {
                        crud.Update = false;
                    }
                    if (item.Delete)
                    {
                        crud.Delete = true;
                    }
                    else
                    {
                        crud.Delete = false;
                    }                 

                    if (item.ADD==false && item.Read==false && item.Update==false && item.Delete == false)
                    {
                        count++;
                       
                    }
                    db.CRUDs.Add(crud);
                    db.SaveChanges();
                    item.Count++;                          
                }

                //if user doesn't choose privilages
                if (count == g.pageCruds.Count)
                {
                    ViewBag.MessageError = "*You Must Choose The Privilages";

                    //solve of duplicated saved group in database
                    List<Crud> c = db.CRUDs.Where(n => n.GroupId == group.GroupId).ToList();
                    if (c != null)
                    {
                        foreach (Crud crd in c)
                        {
                            db.CRUDs.Remove(crd);
                        }
                    }
                    db.Groups.Remove(group);
                    db.SaveChanges();

                    //for loading the data in view
                    List<PageCrud> pcEroor = new List<PageCrud>();
                    foreach (var item in db.Pages.ToList())
                    {
                        pcEroor.Add(new PageCrud { page = item });
                    }
                    GroupRelation groupRelationError = new GroupRelation()
                    {
                        group = new Group(),
                        pageCruds = pcEroor
                    };
                    ViewBag.PageCrud = groupRelationError.pageCruds.Count;
                    return View(groupRelationError);
                }

                //sava changes and redirect to index view
                    db.SaveChanges();
                    return RedirectToAction("Index");
               
            }

            // if user enter data not valid
            List<PageCrud> pc = new List<PageCrud>();
            foreach (var item in db.Pages.ToList())
            {
                pc.Add(new PageCrud { page = item });
            }
            GroupRelation groupRelation = new GroupRelation()
            {
                group = new Group(),
                pageCruds = pc
            };
            ViewBag.PageCrud = groupRelation.pageCruds.Count;
            return View(groupRelation);
        }

        //edit
        public IActionResult EditGroup(int id)
        {
            var admin_id = HttpContext.Session.GetString("adminId");
            var user_id = HttpContext.Session.GetString("userId");

            if (admin_id != null)
            {
                ViewBag.PagesRules = null;
            }
            else if (user_id != null)
            {
                var b = HttpContext.Session.GetString("groupId");
                if (b != null)
                {
                    List<Crud> Rules = db.CRUDs.Where(n => n.GroupId == int.Parse(b)).ToList();
                    ViewBag.PagesRules = Rules;

                }
            }
            List<PageCrud> pageCruds=new List<PageCrud>();
            List<Crud> cruds = db.CRUDs.Where(n => n.GroupId == id).ToList(); 
            
            foreach (var item in cruds)
            {
                pageCruds.Add(new PageCrud { page = item.Page , ADD=item.Add ,Update=item.Update ,Read=item.Read ,Delete=item.Delete });
            }
            GroupRelation groupRelation = new GroupRelation()
            {
                group = db.Groups.Where(n=>n.GroupId==id).FirstOrDefault(),
                pageCruds=pageCruds
            };
            ViewBag.PageCrud = groupRelation.pageCruds.Count;
            return View(groupRelation);
        }
        //edit action
        [HttpPost]
        public IActionResult EditGroup(GroupRelation g)
        {
            if (ModelState.IsValid)
            {
                Group group=db.Groups.Where(n=>n.GroupId==g.group.GroupId).FirstOrDefault();

                //solve duplicated group name
                List<Group> nameValid = db.Groups.ToList();
                if (group.GroupName != g.group.GroupName)
                {
                    foreach (var item in nameValid)
                    {
                        if (g.group.GroupName == item.GroupName)
                        {
                            ViewBag.NameValidation = "*This Group Name Is Already Exist";

                            List<PageCrud> pcEroor = new List<PageCrud>();
                            foreach (var itemV in db.Pages.ToList())
                            {
                                pcEroor.Add(new PageCrud { page = itemV });
                            }
                            GroupRelation groupRelationError = new GroupRelation()
                            {
                                group = new Group(),
                                pageCruds = pcEroor
                            };
                            ViewBag.PageCrud = groupRelationError.pageCruds.Count;
                            return View(groupRelationError);
                        }
                        else
                        {
                            ViewBag.NameValidation = "";
                        }

                    }
                }

                int count = 0;
                //edit group
                group.GroupName=g.group.GroupName;
                foreach(var pageCrud in g.pageCruds)
                {
                   List<Crud> Cruds = db.CRUDs.Where(n => n.GroupId == g.group.GroupId).ToList();
                    foreach(var crud in Cruds)
                    {
                        if (crud.PageId == pageCrud.page.PageId)
                        {
                            crud.Add = pageCrud.ADD;
                            crud.Read = pageCrud.Read;
                            crud.Delete = pageCrud.Delete;
                            crud.Update = pageCrud.Update;

                            //row validation count
                            if (crud.Add == false && crud.Read == false && crud.Update == false && crud.Delete == false)
                            {
                                count++;

                            }
                        }
                          pageCrud.Count++;
                    }

                }

                //if user doesn't choose privilages
                if (count == g.pageCruds.Count)
                {
                    ViewBag.MessageError = "*You Must Choose The Privilages";
                    //for loading the data in view
                    List<PageCrud> pageCrudsErorr = new List<PageCrud>();
                    List<Crud> crudsErorr = db.CRUDs.Where(n => n.GroupId == g.group.GroupId).ToList();

                    foreach (var cr in crudsErorr)
                    {
                        pageCrudsErorr.Add(new PageCrud { page = cr.Page, ADD = cr.Add, Update = cr.Update, Read = cr.Read, Delete = cr.Delete });
                    }
                    GroupRelation groupRelationErorr = new GroupRelation()
                    {
                        group = db.Groups.Where(n => n.GroupId == g.group.GroupId).FirstOrDefault(),
                        pageCruds = pageCrudsErorr
                    };
                    ViewBag.PageCrud = groupRelationErorr.pageCruds.Count;
                    return View(groupRelationErorr);

                }

                db.SaveChanges();
                return RedirectToAction("Index");
            }
        

            // if user enter data not valid
            List<PageCrud> pageCruds = new List<PageCrud>();
            List<Crud> cruds = db.CRUDs.Where(n => n.GroupId == g.group.GroupId).ToList();

            foreach (var cr in cruds)
            {
                pageCruds.Add(new PageCrud { page = cr.Page, ADD = cr.Add, Update = cr.Update, Read = cr.Read, Delete = cr.Delete });
            }
            GroupRelation groupRelation = new GroupRelation()
            {
                group = db.Groups.Where(n => n.GroupId == g.group.GroupId).FirstOrDefault(),
                pageCruds = pageCruds
            };
            ViewBag.PageCrud = groupRelation.pageCruds.Count;
            return View(groupRelation);
        }

        //delete
        public IActionResult Delete(int id)
        {
           Group g=db.Groups.Find(id);
           List<Crud> c=db.CRUDs.Where(n=>n.GroupId==id).ToList();
            if (c != null) 
            {
                foreach (Crud crud in c)
                {
                    db.CRUDs.Remove(crud);
                }
            }
            if (g != null)
            {
                db.Groups.Remove(g);
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
