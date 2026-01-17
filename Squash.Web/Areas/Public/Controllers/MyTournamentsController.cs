using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.DataAccess.Entities;
using Squash.Web.Areas.Public.Models;

namespace Squash.Web.Areas.Public.Controllers
{
    [Area("Public")]
    [Authorize]
    public class MyTournamentsController(IDataContext dataContext, UserManager<IdentityUser> userManager) : Controller
    {
        private readonly IDataContext _dataContext = dataContext;
        private readonly UserManager<IdentityUser> _userManager = userManager;

        [HttpGet]
        [Route("{culture:regex(^bg|en$)}/my-tournaments")]
        public IActionResult Index(string culture)
        {
            var userId = GetCurrentAppUserId();
            if (!userId.HasValue)
            {
                return Forbid();
            }

            var items = _dataContext.Tournaments
                .AsNoTracking()
                .Where(t => t.UserId == userId.Value)
                .OrderByDescending(t => t.StartDate ?? t.DateCreated)
                .Select(t => new MyTournamentListItemViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    IsPublished = t.IsPublished
                })
                .ToList();

            var model = new MyTournamentsIndexViewModel
            {
                Culture = culture,
                Items = items
            };
            return View(model);
        }

        [HttpGet]
        [Route("{culture:regex(^bg|en$)}/my-tournaments/create")]
        public IActionResult Create(string culture)
        {
            var model = BuildEditModel(culture, null);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("{culture:regex(^bg|en$)}/my-tournaments/create")]
        public IActionResult Create(string culture, MyTournamentEditViewModel model)
        {
            var userId = GetCurrentAppUserId();
            if (!userId.HasValue)
            {
                return Forbid();
            }

            model.Culture = culture;
            if (!ModelState.IsValid)
            {
                HydrateCountries(model);
                return View(model);
            }

            var tournament = new Tournament
            {
                Name = model.Name.Trim(),
                Slug = GenerateSlug(model.Name),
                CountryId = model.CountryId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                UserId = userId.Value,
                EntitySourceId = EntitySource.Native,
                IsPublished = false
            };

            _dataContext.Tournaments.Add(tournament);
            _dataContext.SaveChanges();

            return RedirectToAction(nameof(Index), new { culture });
        }

        [HttpGet]
        [Route("{culture:regex(^bg|en$)}/my-tournaments/{id:int}/edit")]
        public IActionResult Edit(string culture, int id)
        {
            var userId = GetCurrentAppUserId();
            if (!userId.HasValue)
            {
                return Forbid();
            }

            var tournament = _dataContext.Tournaments
                .AsNoTracking()
                .FirstOrDefault(t => t.Id == id && t.UserId == userId.Value);
            if (tournament == null)
            {
                return NotFound();
            }

            var model = BuildEditModel(culture, tournament);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("{culture:regex(^bg|en$)}/my-tournaments/{id:int}/edit")]
        public IActionResult Edit(string culture, int id, MyTournamentEditViewModel model)
        {
            var userId = GetCurrentAppUserId();
            if (!userId.HasValue)
            {
                return Forbid();
            }

            model.Culture = culture;
            if (!ModelState.IsValid)
            {
                HydrateCountries(model);
                return View(model);
            }

            var tournament = _dataContext.Tournaments
                .FirstOrDefault(t => t.Id == id && t.UserId == userId.Value);
            if (tournament == null)
            {
                return NotFound();
            }

            tournament.Name = model.Name.Trim();
            tournament.Slug = GenerateSlug(model.Name);
            tournament.CountryId = model.CountryId;
            tournament.StartDate = model.StartDate;
            tournament.EndDate = model.EndDate;

            _dataContext.SaveChanges();

            return RedirectToAction(nameof(Index), new { culture });
        }

        private int? GetCurrentAppUserId()
        {
            var identityUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                return null;
            }

            var userId = _dataContext.Users
                .Where(u => u.IdentityUserId == identityUserId)
                .Select(u => u.Id)
                .FirstOrDefault();

            if (userId == 0)
            {
                var identityUser = _userManager.Users.FirstOrDefault(u => u.Id == identityUserId);
                if (identityUser == null)
                {
                    return null;
                }

                var newUser = new Squash.DataAccess.Entities.User
                {
                    IdentityUserId = identityUserId,
                    Name = identityUser.UserName ?? identityUser.Email ?? "User",
                    Email = identityUser.Email ?? string.Empty,
                    Phone = identityUser.PhoneNumber ?? "N/A",
                    BirthDate = new DateTime(2000, 1, 1),
                    Zip = "0000",
                    City = "Unknown",
                    Address = "Unknown",
                    Verified = identityUser.EmailConfirmed,
                    EmailNotificationsEnabled = false,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    LastOperationUserId = 0
                };
                _dataContext.Users.Add(newUser);
                _dataContext.SaveChanges();
                userId = newUser.Id;
            }

            return userId == 0 ? null : userId;
        }

        private MyTournamentEditViewModel BuildEditModel(string culture, Tournament? tournament)
        {
            var model = new MyTournamentEditViewModel
            {
                Culture = culture
            };

            if (tournament != null)
            {
                model.Id = tournament.Id;
                model.Name = tournament.Name;
                model.CountryId = tournament.CountryId;
                model.StartDate = tournament.StartDate;
                model.EndDate = tournament.EndDate;
            }

            HydrateCountries(model);
            return model;
        }

        private void HydrateCountries(MyTournamentEditViewModel model)
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
        }

        private static string GenerateSlug(string name)
        {
            var words = Regex.Matches(name, @"[\\p{L}\\p{N}]+")
                .Select(match => match.Value.ToLowerInvariant())
                .Where(value => !string.IsNullOrWhiteSpace(value));

            return string.Join("-", words);
        }
    }
}
