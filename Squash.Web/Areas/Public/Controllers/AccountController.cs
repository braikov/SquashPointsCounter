using Microsoft.AspNetCore.Mvc;
using Squash.Web.Areas.Public.Models;

namespace Squash.Web.Areas.Public.Controllers
{
    [Area("Public")]
    public class AccountController : Controller
    {
        [HttpGet]
        [Route("{culture:regex(^bg|en$)}/register")]
        public IActionResult Register(string culture, string? returnUrl)
        {
            var model = new PageViewModel
            {
                Title = "Register"
            };
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }
    }
}
