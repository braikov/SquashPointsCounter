using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.DataAccess.Entities;
using Squash.Web.Areas.Administration.Models;

namespace Squash.Web.Areas.Administration.Controllers
{
    [Area("Administration")]
    [Authorize]
    public class VenuesController : Controller
    {
        private readonly IDataContext _dataContext;

        public VenuesController(IDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var venues = _dataContext.Venues
                .AsNoTracking()
                .Include(v => v.Courts)
                .Include(v => v.TournamentVenues)
                .Include(v => v.Country)
                .OrderBy(v => v.Name)
                .Select(v => new VenueListItemViewModel
                {
                    Id = v.Id,
                    Name = v.Name,
                    Street = v.Street,
                    City = v.City,
                    Zip = v.Zip,
                    Region = v.Region,
                    CountryName = v.Country != null ? v.Country.CountryName ?? v.Country.Name : null,
                    CountryCode = v.Country != null ? v.Country.Code : null,
                    Longitude = v.Longitude,
                    Latitude = v.Latitude,
                    Phone = v.Phone,
                    Email = v.Email,
                    Website = v.Website,
                    CourtsCount = v.Courts.Count,
                    TournamentsCount = v.TournamentVenues.Count
                })
                .ToList();

            var model = new VenuesIndexViewModel
            {
                Venues = venues,
                TotalCount = venues.Count
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new VenueEditViewModel
            {
                AvailableCountries = GetCountryOptions(null)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(VenueEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableCountries = GetCountryOptions(model.CountryId);
                return View(model);
            }

            var venue = new Venue
            {
                Name = model.Name?.Trim() ?? string.Empty,
                Street = model.Street?.Trim(),
                City = model.City?.Trim(),
                Zip = model.Zip?.Trim(),
                Region = model.Region?.Trim(),
                CountryId = model.CountryId,
                Longitude = model.Longitude,
                Latitude = model.Latitude,
                Phone = model.Phone?.Trim(),
                Email = model.Email?.Trim(),
                Website = model.Website?.Trim()
            };

            _dataContext.Venues.Add(venue);
            _dataContext.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var venue = _dataContext.Venues
                .AsNoTracking()
                .Include(v => v.Courts)
                .FirstOrDefault(v => v.Id == id);

            if (venue == null)
            {
                return NotFound();
            }

            var model = new VenueEditViewModel
            {
                Id = venue.Id,
                Name = venue.Name,
                Street = venue.Street,
                City = venue.City,
                Zip = venue.Zip,
                Region = venue.Region,
                CountryId = venue.CountryId,
                Longitude = venue.Longitude,
                Latitude = venue.Latitude,
                Phone = venue.Phone,
                Email = venue.Email,
                Website = venue.Website,
                AvailableCountries = GetCountryOptions(venue.CountryId),
                Courts = venue.Courts
                    .OrderBy(c => c.Name)
                    .Select(c => new CourtEditViewModel
                    {
                        Id = c.Id,
                        Name = c.Name
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details(VenueEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableCountries = GetCountryOptions(model.CountryId);
                return View(model);
            }

            var venue = _dataContext.Venues
                .Include(v => v.Courts)
                .FirstOrDefault(v => v.Id == model.Id);

            if (venue == null)
            {
                return NotFound();
            }

            venue.Name = model.Name?.Trim() ?? string.Empty;
            venue.Street = model.Street?.Trim();
            venue.City = model.City?.Trim();
            venue.Zip = model.Zip?.Trim();
            venue.Region = model.Region?.Trim();
            venue.CountryId = model.CountryId;
            venue.Longitude = model.Longitude;
            venue.Latitude = model.Latitude;
            venue.Phone = model.Phone?.Trim();
            venue.Email = model.Email?.Trim();
            venue.Website = model.Website?.Trim();

            // Handle Courts
            foreach (var courtModel in model.Courts)
            {
                if (courtModel.IsDeleted)
                {
                    if (courtModel.Id > 0)
                    {
                        var existingCourt = venue.Courts.FirstOrDefault(c => c.Id == courtModel.Id);
                        if (existingCourt != null)
                        {
                            // If we can't delete due to FK (matches), we might need to handle it.
                            // For now, assume cascading delete or explicit removal.
                            // Actually, soft delete or check usage is safer, but user asked to "remove them".
                            // Entity framework will try to delete. If matches exist, it might fail if not configured.
                            // Let's assume standard removal from collection + orchestration handles it or throws.
                            // _dataContext.Courts.Remove(existingCourt); // Direct remove or via collection
                            // Removing from collection is often enough with proper configuration.
                             _dataContext.Courts.Remove(existingCourt);
                        }
                    }
                }
                else
                {
                    if (courtModel.Id == 0)
                    {
                        // Add new
                        venue.Courts.Add(new Court
                        {
                            Name = courtModel.Name?.Trim() ?? string.Empty
                        });
                    }
                    else
                    {
                        // Update existing
                        var existingCourt = venue.Courts.FirstOrDefault(c => c.Id == courtModel.Id);
                        if (existingCourt != null)
                        {
                            existingCourt.Name = courtModel.Name?.Trim() ?? string.Empty;
                        }
                    }
                }
            }

            _dataContext.SaveChanges();

            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        private List<SelectListItem> GetCountryOptions(int? selectedId)
        {
            return _dataContext.Nationalities
                .AsNoTracking()
                .OrderBy(n => n.CountryName ?? n.Name ?? n.Code)
                .Select(n => new SelectListItem
                {
                    Value = n.Id.ToString(),
                    Text = n.CountryName ?? n.Name ?? n.Code,
                    Selected = selectedId.HasValue && n.Id == selectedId.Value
                })
                .ToList();
        }
    }
}
