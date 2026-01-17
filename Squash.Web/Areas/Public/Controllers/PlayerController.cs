using Microsoft.AspNetCore.Mvc;
using Squash.Web.Areas.Public.Models;

namespace Squash.Web.Areas.Public.Controllers
{
    [Area("Public")]
    public class PlayerController : Controller
    {
        [HttpGet]
        [Route("{culture:regex(^bg|en$)}/player/home")]
        public IActionResult Home(string culture)
        {
            var model = new PageViewModel
            {
                Title = "Player Home"
            };
            return View(model);
        }
    }
}
