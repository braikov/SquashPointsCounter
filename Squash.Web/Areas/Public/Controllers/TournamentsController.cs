using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Squash.SqlServer;
using Squash.Web.Areas.Public.Models;

namespace Squash.Web.Areas.Public.Controllers
{
    [Area("Public")]
    public class TournamentsController : Controller
    {
        private readonly DataContext _dataContext;

        public TournamentsController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet]
        [Route("{culture:regex(^bg|en$)}/tournaments")]
        public async Task<IActionResult> Index(string culture, CancellationToken cancellationToken)
        {
            var tournaments = await _dataContext.Tournaments
                .AsNoTracking()
                .Where(tournament => tournament.IsPublished)
                .OrderByDescending(tournament => tournament.StartDate ?? tournament.DateCreated)
                .Select(tournament => new TournamentListItemViewModel
                {
                    Id = tournament.Id,
                    Name = tournament.Name,
                    Slug = tournament.Slug,
                    StartDate = tournament.StartDate,
                    EndDate = tournament.EndDate
                })
                .ToListAsync(cancellationToken);

            var model = new TournamentListViewModel
            {
                Culture = culture,
                Items = tournaments
            };

            return View(model);
        }

        [HttpGet]
        [Route("{culture:regex(^bg|en$)}/tournament/{id:int}/{slug?}")]
        public async Task<IActionResult> Details(string culture, int id, string? slug, CancellationToken cancellationToken)
        {
            var tournament = await _dataContext.Tournaments
                .AsNoTracking()
                .Where(item => item.IsPublished && item.Id == id)
                .Select(item => new TournamentDetailsViewModel
                {
                    Id = item.Id,
                    Name = item.Name,
                    Slug = item.Slug,
                    StartDate = item.StartDate,
                    EndDate = item.EndDate,
                    Regulations = item.Regulations
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (tournament == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(tournament.Slug)
                && !string.Equals(tournament.Slug, slug, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Details), new { culture, id, slug = tournament.Slug });
            }

            tournament.Culture = culture;
            return View(tournament);
        }
    }
}
