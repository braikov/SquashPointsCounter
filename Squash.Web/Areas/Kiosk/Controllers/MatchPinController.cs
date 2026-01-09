using Microsoft.AspNetCore.Mvc;

namespace Squash.Web.Areas.Kiosk.Controllers
{
    [Area("Kiosk")]
    public class MatchPinController : Controller
    {
        public IActionResult Index()
        {
            HttpContext.Session.Remove("MatchPin");
            return View();
        }
    }
}
