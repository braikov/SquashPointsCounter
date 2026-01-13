using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Squash.Web.Areas.Identity.Models.Account;

namespace Squash.Web.Areas.Identity.Controllers
{
    [Area("Identity")]
    [AllowAnonymous]
    public class AccountController(SignInManager<IdentityUser> signInManager, ILogger<AccountController> logger) : Controller
    {
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var vm = new LoginViewModel { Email = string.Empty, Password = string.Empty, ReturnUrl = returnUrl };
            return View(vm);
        }

        [HttpGet]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            TempData["AccessDeniedMessage"] = "Access denied. You do not have permission to view the requested resource. Sign in with an account that has access.";
            return RedirectToAction("Login", new { returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                logger.LogInformation("User logged in: {Email}", model.Email);
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Dashboard", new { area = "Administration" });
            }

            if (result.IsLockedOut)
            {
                logger.LogWarning("User account locked out.");
                ModelState.AddModelError(string.Empty, "Account locked out. Try again later.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            logger.LogInformation("User logged out.");
            return RedirectToAction("Login");
        }
    }
}
