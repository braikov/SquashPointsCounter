using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Squash.DataAccess;
using Squash.Web.Areas.Administration.Models;

namespace Squash.Web.Areas.Administration.Controllers
{
    [Area("Administration")]
    [Authorize]
    public class PlayersController : Controller
    {
        private readonly IDataContext _dataContext;

        public PlayersController(IDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet]
        public IActionResult Index(string? country)
        {
            var normalizedCountry = string.IsNullOrWhiteSpace(country)
                ? null
                : country.Trim().ToUpperInvariant();

            var baseQuery = _dataContext.Players
                .AsNoTracking()
                .Include(p => p.Country)
                .AsQueryable();

            var totalCount = baseQuery.Count();

            if (!string.IsNullOrWhiteSpace(normalizedCountry))
            {
                baseQuery = baseQuery.Where(p =>
                    p.Country != null
                    && p.Country.Code != null
                    && p.Country.Code.ToUpper() == normalizedCountry);
            }

            var players = baseQuery
                .OrderBy(p => p.Name)
                .Select(p => new PlayerListItemViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    CountryCode = p.Country != null ? p.Country.Code : string.Empty,
                    CountryName = p.Country != null ? (p.Country.Nationality ?? string.Empty) : string.Empty,
                    FlagUrl = BuildFlagUrl(p.Country != null ? p.Country.Code : null),
                    EsfMemberId = p.EsfMemberId ?? string.Empty
                })
                .ToList();

            var availableCountries = _dataContext.Countries
                .AsNoTracking()
                .Where(n => n.Players.Any())
                .OrderBy(n => n.Code)
                .Select(n => new FilterOption
                {
                    Value = n.Code.ToUpperInvariant(),
                    Text = string.IsNullOrWhiteSpace(n.Nationality)
                        ? n.Code.ToUpperInvariant()
                        : $"{n.Code.ToUpperInvariant()} - {n.Nationality}"
                })
                .ToList();

            var model = new PlayersIndexViewModel
            {
                Players = players,
                FilterCountry = normalizedCountry,
                AvailableCountries = availableCountries,
                TotalCount = totalCount,
                FilteredCount = players.Count
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new PlayerEditViewModel
            {
                Name = string.Empty,
                EsfMemberId = string.Empty,
                AvailableCountries = GetCountryOptions(null)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PlayerEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableCountries = GetCountryOptions(model.CountryId);
                return View(model);
            }

            var player = new Squash.DataAccess.Entities.Player
            {
                Name = model.Name?.Trim() ?? string.Empty,
                CountryId = model.CountryId,
                EsfMemberId = string.IsNullOrWhiteSpace(model.EsfMemberId) ? null : model.EsfMemberId.Trim()
            };

            _dataContext.Players.Add(player);
            _dataContext.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var player = _dataContext.Players
                .AsNoTracking()
                .FirstOrDefault(p => p.Id == id);

            if (player == null)
            {
                return NotFound();
            }

            var model = new PlayerEditViewModel
            {
                Id = player.Id,
                Name = player.Name,
                CountryId = player.CountryId,
                EsfMemberId = player.EsfMemberId,
                AvailableCountries = GetCountryOptions(player.CountryId)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details(PlayerEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableCountries = GetCountryOptions(model.CountryId);
                return View(model);
            }

            var player = _dataContext.Players
                .FirstOrDefault(p => p.Id == model.Id);

            if (player == null)
            {
                return NotFound();
            }

            player.Name = model.Name?.Trim() ?? string.Empty;
            player.CountryId = model.CountryId;
            player.EsfMemberId = string.IsNullOrWhiteSpace(model.EsfMemberId) ? null : model.EsfMemberId.Trim();

            _dataContext.SaveChanges();

            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        private static string BuildFlagUrl(string? nationalityCode)
        {
            if (string.IsNullOrWhiteSpace(nationalityCode))
            {
                return string.Empty;
            }

            return $"/images/flags/{nationalityCode.ToLowerInvariant()}.svg";
        }

        private List<SelectListItem> GetCountryOptions(int? selectedId)
        {
            var items = _dataContext.Countries
                .AsNoTracking()
                .OrderBy(n => n.Code)
                .Select(n => new SelectListItem
                {
                    Value = n.Id.ToString(),
                    Text = string.IsNullOrWhiteSpace(n.Nationality)
                        ? n.Code.ToUpperInvariant()
                        : $"{n.Code.ToUpperInvariant()} - {n.Nationality}"
                })
                .ToList();

            if (selectedId.HasValue)
            {
                var match = items.FirstOrDefault(i => i.Value == selectedId.Value.ToString());
                if (match != null)
                {
                    match.Selected = true;
                }
            }

            return items;
        }
    }
}


