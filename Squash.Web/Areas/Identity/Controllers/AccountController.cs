using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Squash.Identity;
using Squash.Identity.Entities;
using Squash.Shared.Utils;
using Squash.Web.Areas.Identity.Models.Account;
using Squash.Web.Resources.Shared;
using Squash.Identity.Services.Email;

namespace Squash.Web.Areas.Identity.Controllers
{
    [Area("Identity")]
    [AllowAnonymous]
    public class AccountController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        ApplicationDbContext identityContext,
        IEmailSender emailSender,
        IStringLocalizer<Validation> validationLocalizer,
        ILogger<AccountController> logger) : Controller
    {
        private const int EmailSendWindowMinutes = 15;
        private const int EmailSendMaxCount = 3;

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

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user != null && !user.EmailConfirmed)
            {
                ViewData["EmailNotConfirmed"] = true;
                ViewData["UnconfirmedEmail"] = model.Email;
                LogAccountEvent(AccountEventType.LoginBlockedUnconfirmed, model.Email, user.Id);
                return View(model);
            }

            var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                logger.LogInformation("User logged in: {Email}", model.Email);
                LogAccountEvent(AccountEventType.LoginSucceeded, model.Email, user?.Id);
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                if (user != null && await userManager.IsInRoleAsync(user, "Administrator"))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Administration" });
                }

                return Redirect("/");
            }

            if (result.IsLockedOut)
            {
                logger.LogWarning("User account locked out.");
                ModelState.AddModelError(string.Empty, "Account locked out. Try again later.");
                LogAccountEvent(AccountEventType.LoginFailed, model.Email, user?.Id);
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            LogAccountEvent(AccountEventType.LoginFailed, model.Email, user?.Id);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            logger.LogInformation("User logged out.");
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmation(string email, string? returnUrl, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                LogAccountEvent(AccountEventType.ResendConfirmationRequested, email, null);

                if (IsEmailRateLimited(email, AccountEventType.EmailConfirmationSent))
                {
                    TempData["ResendConfirmationRateLimited"] = true;
                    return RedirectToAction("Login", new { returnUrl });
                }

                var user = await userManager.FindByEmailAsync(email);
                if (user != null && !user.EmailConfirmed)
                {
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var code = RandomCodeGenerator.GenerateSixCharCode();
                    identityContext.ShortCodeToTokens.Add(new ShortCodeToToken
                    {
                        Email = email,
                        Code = code,
                        Token = token
                    });
                    await identityContext.SaveChangesAsync(cancellationToken);

                    var confirmUrl = Url.Action(
                        "ConfirmEmail",
                        "Account",
                        new { area = "Identity", email, code },
                        Request.Scheme);
                    if (!string.IsNullOrWhiteSpace(confirmUrl))
                    {
                        await emailSender.SendRegistrationVerificationAsync(email, confirmUrl, cancellationToken);
                        LogAccountEvent(AccountEventType.EmailConfirmationSent, email, user.Id);
                    }
                }
            }

            TempData["ResendConfirmationSuccess"] = true;
            return RedirectToAction("Login", new { returnUrl });
        }

        [HttpGet]
        public IActionResult ForgotPassword(string? returnUrl = null)
        {
            var model = new ForgotPasswordViewModel { Email = string.Empty, ReturnUrl = returnUrl };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (IsEmailRateLimited(model.Email, AccountEventType.PasswordResetSent))
            {
                ModelState.AddModelError(string.Empty, validationLocalizer["RateLimitExceeded"]);
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                var code = RandomCodeGenerator.GenerateSixCharCode();
                identityContext.ShortCodeToTokens.Add(new ShortCodeToToken
                {
                    Email = model.Email,
                    Code = code,
                    Token = resetToken
                });
                await identityContext.SaveChangesAsync(cancellationToken);

                var resetUrl = Url.Action(
                    "ResetPassword",
                    "Account",
                    new { area = "Identity", email = model.Email, code },
                    Request.Scheme);
                if (!string.IsNullOrWhiteSpace(resetUrl))
                {
                    await emailSender.SendPasswordResetAsync(model.Email, resetUrl, cancellationToken);
                    LogAccountEvent(AccountEventType.PasswordResetSent, model.Email, user.Id);
                }
            }

            LogAccountEvent(AccountEventType.PasswordResetRequested, model.Email, user?.Id);
            var confirmation = new ForgotPasswordConfirmationViewModel { ReturnUrl = model.ReturnUrl };
            return View("ForgotPasswordConfirmation", confirmation);
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string code, string? returnUrl = null)
        {
            var model = new ResetPasswordViewModel
            {
                Email = email,
                Code = code,
                ReturnUrl = returnUrl
            };

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                ModelState.AddModelError(string.Empty, validationLocalizer["InvalidCode"]);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string email, string code, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                var failModel = new ConfirmEmailViewModel { Success = false };
                return View(failModel);
            }

            var entry = identityContext.ShortCodeToTokens
                .FirstOrDefault(x => x.Email == email && x.Code == code);
            if (entry == null)
            {
                var failModel = new ConfirmEmailViewModel { Success = false };
                return View(failModel);
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                var failModel = new ConfirmEmailViewModel { Success = false };
                return View(failModel);
            }

            var result = await userManager.ConfirmEmailAsync(user, entry.Token);
            if (!result.Succeeded)
            {
                var failModel = new ConfirmEmailViewModel { Success = false };
                return View(failModel);
            }

            identityContext.ShortCodeToTokens.Remove(entry);
            await identityContext.SaveChangesAsync(cancellationToken);

            LogAccountEvent(AccountEventType.EmailConfirmed, email, user.Id);

            TempData["EmailConfirmedSuccess"] = true;
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var entry = identityContext.ShortCodeToTokens
                .FirstOrDefault(x => x.Email == model.Email && x.Code == model.Code);
            if (entry == null)
            {
                ModelState.AddModelError(string.Empty, validationLocalizer["InvalidCode"]);
                return View(model);
            }

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, validationLocalizer["InvalidCode"]);
                return View(model);
            }

            var resetResult = await userManager.ResetPasswordAsync(user, entry.Token, model.Password);
            if (!resetResult.Succeeded)
            {
                foreach (var error in resetResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            identityContext.ShortCodeToTokens.Remove(entry);
            await identityContext.SaveChangesAsync(cancellationToken);

            LogAccountEvent(AccountEventType.PasswordResetCompleted, model.Email, user.Id);

            await signInManager.SignInAsync(user, isPersistent: false);

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return Redirect("/");
        }

        private bool IsEmailRateLimited(string email, string eventType)
        {
            var windowStart = DateTime.UtcNow.AddMinutes(-EmailSendWindowMinutes);
            return identityContext.AccountEvents
                .Count(e => e.Email == email && e.EventType == eventType && e.DateCreated >= windowStart) >= EmailSendMaxCount;
        }

        private void LogAccountEvent(string eventType, string email, string? userId)
        {
            identityContext.AccountEvents.Add(new AccountEvent
            {
                EventType = eventType,
                Email = email,
                UserId = userId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });
            identityContext.SaveChanges();
        }
    }
}
