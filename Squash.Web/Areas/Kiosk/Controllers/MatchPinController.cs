using Microsoft.AspNetCore.Mvc;

namespace Squash.Web.Areas.Kiosk.Controllers
{
    [Area("Kiosk")]
    public class MatchPinController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
