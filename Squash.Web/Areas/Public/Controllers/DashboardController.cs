using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Squash.Web.Areas.Public.Models;

namespace Squash.Web.Areas.Public.Controllers
{
    [Area("Public")]
    [Authorize]
    public class DashboardController : Controller
    {
        [HttpGet]
        [Route("{culture:regex(^bg|en$)}/dashboard")]
        public IActionResult Index(string culture)
        {
            var model = new PageViewModel
            {
                Title = "Dashboard"
            };
            return View(model);
        }
    }
}
