using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.Web.Areas.Administration.Models;

namespace Squash.Web.Areas.Administration.Controllers
{
    [Area("Administration")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDataContext _dataContext;

        public DashboardController(IDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.Date.AddDays(-30);

            var model = new DashboardViewModel
            {
                TotalUsers = await _dataContext.Users.AsNoTracking().LongCountAsync(),
                TotalTournaments = await _dataContext.Tournaments.AsNoTracking().LongCountAsync()
            };

            model.NewUsersLast30Days = await _dataContext.Users.AsNoTracking()
                .Where(u => u.DateCreated >= thirtyDaysAgo)
                .LongCountAsync();

            model.NewTournamentsLast30Days = await _dataContext.Tournaments.AsNoTracking()
                .Where(t => t.DateCreated >= thirtyDaysAgo)
                .LongCountAsync();

            var usersGrouped = await _dataContext.Users.AsNoTracking()
                .Where(u => u.DateCreated >= thirtyDaysAgo)
                .GroupBy(u => u.DateCreated.Date)
                .Select(g => new { Date = g.Key, Count = g.LongCount() })
                .ToListAsync();

            var tournamentsGrouped = await _dataContext.Tournaments.AsNoTracking()
                .Where(t => t.DateCreated >= thirtyDaysAgo)
                .GroupBy(t => t.DateCreated.Date)
                .Select(g => new { Date = g.Key, Count = g.LongCount() })
                .ToListAsync();

            for (var i = 29; i >= 0; i -= 1)
            {
                var date = now.Date.AddDays(-i);
                var label = date.ToString("MM/dd");
                var usersCount = usersGrouped.FirstOrDefault(x => x.Date == date)?.Count ?? 0;
                var tournamentsCount = tournamentsGrouped.FirstOrDefault(x => x.Date == date)?.Count ?? 0;
                model.UserSignupsByDay[label] = usersCount;
                model.TournamentCreationsByDay[label] = tournamentsCount;
            }

            return View(model);
        }
    }
}
