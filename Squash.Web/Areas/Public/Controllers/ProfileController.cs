using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.DataAccess.Entities;
using Squash.Web.Areas.Public.Models;
using Microsoft.Extensions.Localization;
using Squash.Web.Resources.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Squash.Web.Areas.Public.Controllers
{
    [Area("Public")]
    [Authorize]
    public class ProfileController(
        IDataContext dataContext,
        UserManager<IdentityUser> userManager,
        IStringLocalizerFactory localizerFactory,
        IWebHostEnvironment webHostEnvironment) : Controller
    {
        private const long MaxAvatarBytes = 1572864; // 1.5 MB
        private const int MinAvatarSize = 256;
        private const int MaxAvatarSize = 2048;
        private const int TargetAvatarSize = 512;
        private const int AvatarJpegQuality = 85;

        private readonly IDataContext _dataContext = dataContext;
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly IStringLocalizer _validationLocalizer =
            localizerFactory.Create("Shared.Validation", "Squash.Web");
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

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
            EnsurePlayer(appUser);
            var avatarError = ValidateAvatar(model);
            if (!string.IsNullOrWhiteSpace(avatarError))
            {
                ModelState.AddModelError(nameof(ProfileEditViewModel.AvatarFile), avatarError);
            }
            if (!ModelState.IsValid)
            {
                HydrateLists(model);
                model.CurrentAvatarUrl = appUser.Player?.PictureUrl;
                model.AvatarInitials = BuildInitials(appUser.Name, appUser.FirstName, appUser.LastName);
                return View(model);
            }

            SaveAvatar(model, appUser.Player!);

            appUser.FirstName = model.FirstName.Trim();
            appUser.LastName = model.LastName.Trim();
            appUser.Name = $"{model.FirstName} {model.LastName}".Trim();
            appUser.Email = model.Email.Trim();
            appUser.BirthDate = model.BirthDate ?? appUser.BirthDate;
            appUser.Gender = model.Gender;
            appUser.CountryId = model.CountryId;
            appUser.PreferredSport = model.Sport;
            appUser.DateUpdated = DateTime.UtcNow;

            if (appUser.Player != null)
            {
                appUser.Player.Name = appUser.Name;
                appUser.Player.CountryId = appUser.CountryId;
            }

            _dataContext.SaveChanges();

            UpdateIdentityUserEmail(appUser);
            UpdateIdentityUserPassword(model);

            HydrateLists(model);
            if (ModelState.IsValid)
            {
                model.SaveSucceeded = true;
            }
            model.CurrentAvatarUrl = appUser.Player?.PictureUrl;
            model.AvatarInitials = BuildInitials(appUser.Name, appUser.FirstName, appUser.LastName);
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
                Sport = appUser.PreferredSport ?? "Squash",
                CurrentAvatarUrl = appUser.Player?.PictureUrl,
                AvatarInitials = BuildInitials(appUser.Name, appUser.FirstName, appUser.LastName)
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

            return _dataContext.Users
                .Include(u => u.Player)
                .FirstOrDefault(u => u.IdentityUserId == identityUserId);
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

        private static string BuildInitials(string? name, string? firstName, string? lastName)
        {
            var source = string.IsNullOrWhiteSpace(name)
                ? $"{firstName} {lastName}".Trim()
                : name;

            if (string.IsNullOrWhiteSpace(source))
            {
                return "??";
            }

            var parts = source.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 1)
            {
                return string.Concat(parts[0].Take(2)).ToUpperInvariant();
            }

            var first = parts[0][0];
            var second = parts[1][0];
            return $"{char.ToUpperInvariant(first)}{char.ToUpperInvariant(second)}";
        }

        private void EnsurePlayer(Squash.DataAccess.Entities.User appUser)
        {
            if (appUser.Player != null)
            {
                return;
            }

            Player? player = null;
            if (appUser.PlayerId.HasValue)
            {
                player = _dataContext.Players.FirstOrDefault(p => p.Id == appUser.PlayerId.Value);
            }

            if (player == null)
            {
                player = new Player
                {
                    Name = appUser.Name,
                    UserId = appUser.Id,
                    CountryId = appUser.CountryId
                };
                _dataContext.Players.Add(player);
                _dataContext.SaveChanges();
                appUser.PlayerId = player.Id;
                _dataContext.SaveChanges();
            }

            appUser.Player = player;
        }

        private string? ValidateAvatar(ProfileEditViewModel model)
        {
            var file = model.AvatarFile;
            if (file == null || file.Length == 0)
            {
                return null;
            }

            if (file.Length > MaxAvatarBytes)
            {
                return _validationLocalizer["FileTooLarge"];
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowedExtensions.Contains(extension))
            {
                return _validationLocalizer["InvalidImageType"];
            }

            try
            {
                using var image = Image.Load(file.OpenReadStream());
                if (image.Width < MinAvatarSize || image.Height < MinAvatarSize)
                {
                    return _validationLocalizer["ImageTooSmall"];
                }

                if (image.Width > MaxAvatarSize || image.Height > MaxAvatarSize)
                {
                    return _validationLocalizer["ImageTooLarge"];
                }
            }
            catch
            {
                return _validationLocalizer["InvalidImageFile"];
            }

            return null;
        }

        private void SaveAvatar(ProfileEditViewModel model, Player player)
        {
            var file = model.AvatarFile;
            if (file == null || file.Length == 0)
            {
                return;
            }

            var uploadRoot = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(uploadRoot);

            var fileName = $"player-{player.Id}-{Guid.NewGuid():N}.jpg";
            var filePath = Path.Combine(uploadRoot, fileName);

            using (var image = Image.Load(file.OpenReadStream()))
            {
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(TargetAvatarSize, TargetAvatarSize),
                    Mode = ResizeMode.Crop
                }));

                image.Save(filePath, new JpegEncoder { Quality = AvatarJpegQuality });
            }

            if (!string.IsNullOrWhiteSpace(player.PictureUrl) &&
                player.PictureUrl.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
            {
                var oldPath = Path.Combine(_webHostEnvironment.WebRootPath, player.PictureUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                {
                    System.IO.File.Delete(oldPath);
                }
            }

            player.PictureUrl = $"/uploads/avatars/{fileName}";
        }
    }
}
