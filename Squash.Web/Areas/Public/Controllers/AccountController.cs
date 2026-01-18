using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.Identity;
using Squash.Identity.Entities;
using Squash.Shared.Utils;
using Squash.Web.Areas.Public.Models;
using Microsoft.Extensions.Localization;
using Squash.Web.Resources.Shared;
using Squash.Identity.Services.Email;

namespace Squash.Web.Areas.Public.Controllers
{
    [Area("Public")]
    public class AccountController(
        IDataContext dataContext,
        ApplicationDbContext identityContext,
        UserManager<IdentityUser> userManager,
        IEmailSender emailSender,
        IStringLocalizerFactory localizerFactory) : Controller
    {
        private readonly IDataContext _dataContext = dataContext;
        private readonly ApplicationDbContext _identityContext = identityContext;
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly IEmailSender _emailSender = emailSender;
        private readonly IStringLocalizer _validationLocalizer =
            localizerFactory.Create("Shared.Validation", "Squash.Web");

        private const int EmailSendWindowMinutes = 15;
        private const int EmailSendMaxCount = 3;

        [HttpGet]
        [Route("{culture:regex(^bg|en$)}/register")]
        public IActionResult Register(string culture, string? returnUrl)
        {
            var model = BuildRegisterViewModel(culture, returnUrl);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("{culture:regex(^bg|en$)}/register")]
        public async Task<IActionResult> Register(string culture, RegisterViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                HydrateRegisterLists(model);
                return View(model);
            }

            if (IsEmailRateLimited(model.Email, AccountEventType.EmailConfirmationSent))
            {
                ModelState.AddModelError(string.Empty, _validationLocalizer["RateLimitExceeded"]);
                HydrateRegisterLists(model);
                return View(model);
            }

            var identityUser = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = false
            };

            var createResult = await _userManager.CreateAsync(identityUser, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                HydrateRegisterLists(model);
                return View(model);
            }

            var appUser = new Squash.DataAccess.Entities.User
            {
                IdentityUserId = identityUser.Id,
                Name = $"{model.FirstName} {model.LastName}".Trim(),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Phone = "N/A",
                BirthDate = model.BirthDate!.Value,
                Gender = model.Gender,
                CountryId = model.CountryId,
                PreferredSport = model.Sport,
                PreferredLanguage = ResolveLanguageCode(culture),
                Zip = "0000",
                City = "Unknown",
                Address = "Unknown",
                Verified = false,
                EmailNotificationsEnabled = true,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow
            };
            _dataContext.Users.Add(appUser);
            _dataContext.SaveChanges();

            var player = new Squash.DataAccess.Entities.Player
            {
                Name = appUser.Name,
                UserId = appUser.Id,
                CountryId = model.CountryId
            };
            _dataContext.Players.Add(player);
            _dataContext.SaveChanges();

            appUser.PlayerId = player.Id;
            _dataContext.SaveChanges();

            if (string.IsNullOrWhiteSpace(player.ImaId))
            {
                player.ImaId = ImaIdGenerator.GenerateForPlayerId(player.Id);
                _dataContext.SaveChanges();
            }

            var code = RandomCodeGenerator.GenerateSixCharCode();
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
            _identityContext.ShortCodeToTokens.Add(new ShortCodeToToken
            {
                Email = model.Email,
                Code = code,
                Token = token
            });
            await _identityContext.SaveChangesAsync(cancellationToken);

            var verificationUrl = Url.Action("ConfirmEmail", "Account", new { area = "Identity", email = model.Email, code }, Request.Scheme);
            if (!string.IsNullOrWhiteSpace(verificationUrl))
            {
                LogAccountEvent(AccountEventType.RegistrationRequested, model.Email, identityUser.Id);
                await _emailSender.SendRegistrationVerificationAsync(model.Email, verificationUrl, cancellationToken);
                LogAccountEvent(AccountEventType.EmailConfirmationSent, model.Email, identityUser.Id);
            }

            var confirmationModel = new RegisterConfirmationViewModel
            {
                Culture = culture
            };
            return View("RegisterConfirmation", confirmationModel);
        }

        private RegisterViewModel BuildRegisterViewModel(string culture, string? returnUrl)
        {
            var model = new RegisterViewModel
            {
                Title = "Register",
                Culture = culture,
                ReturnUrl = returnUrl
            };

            HydrateRegisterLists(model);
            return model;
        }

        private void HydrateRegisterLists(RegisterViewModel model)
        {
            model.Countries = _dataContext.Countries
                .AsNoTracking()
                .OrderBy(n => n.CountryName)
                .Select(n => new SelectListItem
                {
                    Value = n.Id.ToString(),
                    Text = n.CountryName
                })
                .ToList();

            model.Genders = new List<SelectListItem>
            {
                new SelectListItem { Value = "Male", Text = "Male" },
                new SelectListItem { Value = "Female", Text = "Female" },
                new SelectListItem { Value = "Other", Text = "Other" }
            };

            model.Sports = new List<SelectListItem>
            {
                new SelectListItem { Value = "Squash", Text = "Squash" }
            };
        }

        private static string ResolveLanguageCode(string culture)
        {
            return culture.Equals("bg", StringComparison.OrdinalIgnoreCase) ? "bg-BG" : "en-GB";
        }

        private bool IsEmailRateLimited(string email, string eventType)
        {
            var windowStart = DateTime.UtcNow.AddMinutes(-EmailSendWindowMinutes);
            return _identityContext.AccountEvents
                .Count(e => e.Email == email && e.EventType == eventType && e.DateCreated >= windowStart) >= EmailSendMaxCount;
        }

        private void LogAccountEvent(string eventType, string email, string? userId)
        {
            _identityContext.AccountEvents.Add(new AccountEvent
            {
                EventType = eventType,
                Email = email,
                UserId = userId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            });
            _identityContext.SaveChanges();
        }
    }
}

