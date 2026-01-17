using Microsoft.AspNetCore.Mvc;

namespace Squash.Web.Areas.Public.Controllers
{
    [Area("Public")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var model = new Models.PageViewModel();
            return View(model);
        }
    }
}
