using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Squash.DataAccess;
using Squash.Web.Areas.Administration.Models;

namespace Squash.Web.Areas.Administration.Controllers
{
    [Area("Administration")]
    [Authorize]
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IDataContext _dataContext;

        public UsersController(UserManager<IdentityUser> userManager, IDataContext dataContext)
        {
            _userManager = userManager;
            _dataContext = dataContext;
        }

        public async Task<IActionResult> Index()
        {
            const string adminRole = "Administrator";
            var adminUsers = await _userManager.GetUsersInRoleAsync(adminRole);
            var adminIds = adminUsers.Select(u => u.Id).ToHashSet();

            var users = _userManager.Users
                .Where(u => !adminIds.Contains(u.Id))
                .OrderBy(u => u.Email)
                .Select(u => new UserListItemViewModel
                {
                    Id = u.Id,
                    Name = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Date = string.Empty,
                    Verified = u.EmailConfirmed ? "Yes" : "No"
                })
                .ToList();

            return View(users);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new UserCreateViewModel { Email = string.Empty, Password = string.Empty, ConfirmPassword = string.Empty });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _userManager.FindByEmailAsync(model.Email);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email already exists.");
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var identityUser = _userManager.Users.FirstOrDefault(u => u.Id == id);
            if (identityUser == null)
            {
                return NotFound();
            }

            var appUser = _dataContext.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.IdentityUserId == identityUser.Id);

            var model = new UserDetailsViewModel
            {
                Id = identityUser.Id,
                IdentityUserId = identityUser.Id,
                Email = identityUser.Email ?? string.Empty,
                UserName = identityUser.UserName ?? string.Empty,
                PhoneNumber = identityUser.PhoneNumber ?? string.Empty,
                EmailConfirmed = identityUser.EmailConfirmed ? "Yes" : "No",
                Verified = appUser?.Verified == true ? "Yes" : "No",
                VerificationDate = appUser?.VerificationDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                Name = appUser?.Name ?? string.Empty,
                Phone = appUser?.Phone ?? string.Empty,
                BirthDate = appUser?.BirthDate != null && appUser.BirthDate != DateTime.MinValue
                    ? appUser.BirthDate.ToString("yyyy-MM-dd")
                    : string.Empty,
                Zip = appUser?.Zip ?? string.Empty,
                City = appUser?.City ?? string.Empty,
                Address = appUser?.Address ?? string.Empty,
                BadgeId = appUser?.BadgeId ?? string.Empty
            };

            return View(model);
        }
    }
}
