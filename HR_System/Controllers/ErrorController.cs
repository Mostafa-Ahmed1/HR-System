using Microsoft.AspNetCore.Mvc;

namespace HR_System.Controllers
{
    public class ErrorController : Controller
    {

        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Uh oh, this page could not be found";
                    ViewBag.ErrorNumber = "404";
                    break;
                case 405:
                    ViewBag.ErrorMessage = "Method not allowed. Contact administrator";
                    ViewBag.ErrorNumber = "405";
                    break;
                case 401:
                    ViewBag.ErrorMessage = "You do not have acccess to this page. Please make sure you are logged in, or contact your administrator.";
                    ViewBag.ErrorNumber = "401";
                    break;
                case 500:
                    ViewBag.ErrorMessage = "Internal Server Error. Please contact administrator.";
                    ViewBag.ErrorNumber = "500";
                    break;
                case 403:
                    ViewBag.ErrorMessage = "Forbidden. Please contact administrator.";
                    ViewBag.ErrorNumber = "403";
                    break;
                case 503:
                    ViewBag.ErrorMessage = "Service unavailable. Please contact administrator";
                    ViewBag.ErrorNumber = "503";
                    break;
                case 504:
                    ViewBag.ErrorMessage = "Gateway Timeout. Please contact administrator";
                    ViewBag.ErrorNumber = "504";
                    break;
                case 001:
                    ViewBag.ErrorMessage = "This link has expired.";
                    ViewBag.ErrorNumber = "Oh no!";
                    break;

               

            }
           // return View("NotFound");
            return View(401);
        }
        public IActionResult Index()
        {
            return View();
        }
    }
}
