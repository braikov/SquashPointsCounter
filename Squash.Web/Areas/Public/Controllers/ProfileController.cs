using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.Web.Areas.Public.Models;
using Microsoft.Extensions.Localization;
using Squash.Web.Resources.Shared;

namespace Squash.Web.Areas.Public.Controllers
{
    [Area("Public")]
    [Authorize]
    public class ProfileController(
        IDataContext dataContext,
        UserManager<IdentityUser> userManager,
        IStringLocalizer<Validation> validationLocalizer) : Controller
    {
        private readonly IDataContext _dataContext = dataContext;
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly IStringLocalizer<Validation> _validationLocalizer = validationLocalizer;

        [HttpGet]
        [Route("{culture:regex(^bg|en$)}/profile")]
        public IActionResult Index(string culture)
        {
            var appUser = GetCurrentAppUser();
            if (appUser == null)
            {
                return Forbid();
            }

            var model = BuildProfileViewModel(culture, appUser);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("{culture:regex(^bg|en$)}/profile")]
        public IActionResult Index(string culture, ProfileEditViewModel model)
        {
            var appUser = GetCurrentAppUser();
            if (appUser == null)
            {
                return Forbid();
            }

            model.Culture = culture;
            ValidatePasswords(model);
            if (!ModelState.IsValid)
            {
                HydrateLists(model);
                return View(model);
            }

            appUser.FirstName = model.FirstName.Trim();
            appUser.LastName = model.LastName.Trim();
            appUser.Name = $"{model.FirstName} {model.LastName}".Trim();
            appUser.Email = model.Email.Trim();
            appUser.BirthDate = model.BirthDate ?? appUser.BirthDate;
            appUser.Gender = model.Gender;
            appUser.CountryId = model.CountryId;
            appUser.PreferredSport = model.Sport;
            appUser.DateUpdated = DateTime.UtcNow;

            if (appUser.PlayerId.HasValue)
            {
                var player = _dataContext.Players.FirstOrDefault(p => p.Id == appUser.PlayerId.Value);
                if (player != null)
                {
                    player.Name = appUser.Name;
                    player.CountryId = appUser.CountryId;
                }
            }

            _dataContext.SaveChanges();

            UpdateIdentityUserEmail(appUser);
            UpdateIdentityUserPassword(model);

            HydrateLists(model);
            model.Password = string.Empty;
            model.ConfirmPassword = string.Empty;
            ModelState.Remove(nameof(ProfileEditViewModel.Password));
            ModelState.Remove(nameof(ProfileEditViewModel.ConfirmPassword));
            return View(model);
        }

        private ProfileEditViewModel BuildProfileViewModel(string culture, Squash.DataAccess.Entities.User appUser)
        {
            var model = new ProfileEditViewModel
            {
                Culture = culture,
                Email = appUser.Email,
                FirstName = appUser.FirstName ?? string.Empty,
                LastName = appUser.LastName ?? string.Empty,
                BirthDate = appUser.BirthDate,
                Gender = appUser.Gender ?? string.Empty,
                CountryId = appUser.CountryId ?? 0,
                Sport = appUser.PreferredSport ?? "Squash"
            };

            HydrateLists(model);
            return model;
        }

        private Squash.DataAccess.Entities.User? GetCurrentAppUser()
        {
            var identityUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                return null;
            }

            return _dataContext.Users.FirstOrDefault(u => u.IdentityUserId == identityUserId);
        }

        private void ValidatePasswords(ProfileEditViewModel model)
        {
            var hasPassword = !string.IsNullOrWhiteSpace(model.Password);
            var hasConfirm = !string.IsNullOrWhiteSpace(model.ConfirmPassword);

            if (!hasPassword && !hasConfirm)
            {
                ModelState.Remove(nameof(ProfileEditViewModel.Password));
                ModelState.Remove(nameof(ProfileEditViewModel.ConfirmPassword));
                return;
            }

            if (!hasPassword)
            {
                ModelState.AddModelError(nameof(ProfileEditViewModel.Password), _validationLocalizer["Required"]);
            }

            if (!hasConfirm)
            {
                ModelState.AddModelError(nameof(ProfileEditViewModel.ConfirmPassword), _validationLocalizer["Required"]);
            }
        }

        private void UpdateIdentityUserEmail(Squash.DataAccess.Entities.User appUser)
        {
            var identityUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                return;
            }

            var identityUser = _userManager.Users.FirstOrDefault(u => u.Id == identityUserId);
            if (identityUser == null)
            {
                return;
            }

            var newEmail = appUser.Email.Trim();
            if (!string.Equals(identityUser.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                identityUser.Email = newEmail;
                identityUser.UserName = newEmail;
                _userManager.UpdateAsync(identityUser).GetAwaiter().GetResult();
            }
        }

        private void UpdateIdentityUserPassword(ProfileEditViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password) || string.IsNullOrWhiteSpace(model.ConfirmPassword))
            {
                return;
            }

            var identityUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                return;
            }

            var identityUser = _userManager.Users.FirstOrDefault(u => u.Id == identityUserId);
            if (identityUser == null)
            {
                return;
            }

            var token = _userManager.GeneratePasswordResetTokenAsync(identityUser).GetAwaiter().GetResult();
            var result = _userManager.ResetPasswordAsync(identityUser, token, model.Password).GetAwaiter().GetResult();
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
        }

        private void HydrateLists(ProfileEditViewModel model)
        {
            model.Countries = _dataContext.Countries
                .AsNoTracking()
                .OrderBy(c => c.CountryName ?? c.Nationality ?? c.Code)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.CountryName ?? c.Nationality ?? c.Code,
                    Selected = c.Id == model.CountryId
                })
                .ToList();

            model.Genders = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new() { Value = "Male", Text = "Male" },
                new() { Value = "Female", Text = "Female" }
            };

            model.Sports = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new() { Value = "Squash", Text = "Squash" }
            };
        }
    }
}
