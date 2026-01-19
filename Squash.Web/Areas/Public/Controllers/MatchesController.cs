using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.Web.Areas.Public.Models;

namespace Squash.Web.Areas.Public.Controllers
{
    [Area("Public")]
    [Authorize]
    public class MatchesController : Controller
    {
        private readonly IDataContext _dataContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const int PageSize = 10;

        public MatchesController(
            IDataContext dataContext,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _dataContext = dataContext;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        [Route("{culture:regex(^bg|en$)}/matches")]
        public async Task<IActionResult> Index(
            string culture,
            int upcomingPage = 1,
            int pastPage = 1,
            CancellationToken cancellationToken = default)
        {
            var playerId = await GetCurrentPlayerIdAsync();
            if (!playerId.HasValue)
            {
                return Forbid();
            }

            var now = DateTime.UtcNow.Date;

            var allMatches = await _dataContext.Matches
                .AsNoTracking()
                .Include(m => m.Player1)
                    .ThenInclude(p => p!.Country)
                .Include(m => m.Player2)
                    .ThenInclude(p => p!.Country)
                .Include(m => m.Tournament)
                    .ThenInclude(t => t.Country)
                .Include(m => m.Draw)
                .Include(m => m.Round)
                .Include(m => m.Court)
                .Include(m => m.TournamentDay)
                .Include(m => m.Games)
                .Where(m => m.Player1Id == playerId.Value || m.Player2Id == playerId.Value)
                .ToListAsync(cancellationToken);

            var upcomingMatches = allMatches
                .Where(m => !m.WinnerPlayerId.HasValue && GetMatchDate(m) >= now)
                .OrderBy(m => GetMatchDate(m))
                .ThenBy(m => m.StartTime)
                .ToList();

            var pastMatches = allMatches
                .Where(m => m.WinnerPlayerId.HasValue || GetMatchDate(m) < now)
                .OrderByDescending(m => GetMatchDate(m))
                .ThenByDescending(m => m.StartTime)
                .ToList();

            var upcomingCount = upcomingMatches.Count;
            var pastCount = pastMatches.Count;

            var upcomingTotalPages = (int)Math.Ceiling((double)upcomingCount / PageSize);
            var pastTotalPages = (int)Math.Ceiling((double)pastCount / PageSize);

            upcomingPage = Math.Max(1, Math.Min(upcomingPage, Math.Max(1, upcomingTotalPages)));
            pastPage = Math.Max(1, Math.Min(pastPage, Math.Max(1, pastTotalPages)));

            var pagedUpcoming = upcomingMatches
                .Skip((upcomingPage - 1) * PageSize)
                .Take(PageSize)
                .Select(m => MapToViewModel(m))
                .ToList();

            var pagedPast = pastMatches
                .Skip((pastPage - 1) * PageSize)
                .Take(PageSize)
                .Select(m => MapToViewModel(m))
                .ToList();

            var model = new MatchesIndexViewModel
            {
                Culture = culture,
                UpcomingMatches = pagedUpcoming,
                UpcomingMatchesCount = upcomingCount,
                UpcomingCurrentPage = upcomingPage,
                UpcomingTotalPages = upcomingTotalPages,
                PastMatches = pagedPast,
                PastMatchesCount = pastCount,
                PastCurrentPage = pastPage,
                PastTotalPages = pastTotalPages,
                PageSize = PageSize
            };

            return View(model);
        }

        private async Task<int?> GetCurrentPlayerIdAsync()
        {
            var identityUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                return null;
            }

            var user = await _dataContext.Users
                .AsNoTracking()
                .Include(u => u.Player)
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);

            return user?.Player?.Id;
        }

        private MatchListItemViewModel MapToViewModel(Squash.DataAccess.Entities.Match match)
        {
            return new MatchListItemViewModel
            {
                Id = match.Id,
                Player1Name = GetPlayerName(match.Player1),
                Player1FlagUrl = ResolveFlagUrl(match.Player1?.Country?.Code),
                Player2Name = GetPlayerName(match.Player2),
                Player2FlagUrl = ResolveFlagUrl(match.Player2?.Country?.Code),
                Player1IsWinner = match.WinnerPlayerId.HasValue && match.WinnerPlayerId == match.Player1Id,
                Player2IsWinner = match.WinnerPlayerId.HasValue && match.WinnerPlayerId == match.Player2Id,
                TournamentName = match.Tournament?.Name ?? string.Empty,
                TournamentFlagUrl = ResolveFlagUrl(match.Tournament?.Country?.Code),
                MatchDate = GetMatchDate(match),
                StartTime = match.StartTime,
                ScoreLine = GetMatchScoreLine(match),
                DrawName = match.Draw?.Name,
                RoundName = match.Round?.Name,
                CourtName = match.Court?.Name
            };
        }

        private static string GetPlayerName(Squash.DataAccess.Entities.Player? player)
        {
            return string.IsNullOrWhiteSpace(player?.Name) ? "TBD" : player!.Name;
        }

        private string? ResolveFlagUrl(string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
            {
                return null;
            }

            var upperName = $"{countryCode}.svg";
            var upperPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "flags", upperName);
            if (System.IO.File.Exists(upperPath))
            {
                return $"/images/flags/{upperName}";
            }

            var lowerName = $"{countryCode.ToLowerInvariant()}.svg";
            var lowerPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "flags", lowerName);
            if (System.IO.File.Exists(lowerPath))
            {
                return $"/images/flags/{lowerName}";
            }

            return null;
        }

        private static DateTime? GetMatchDate(Squash.DataAccess.Entities.Match match)
        {
            if (match.TournamentDay?.Date != default)
            {
                return match.TournamentDay.Date;
            }

            if (match.Tournament?.StartDate.HasValue == true)
            {
                return match.Tournament.StartDate.Value.Date;
            }

            return match.DateCreated.Date;
        }

        private static string? GetMatchScoreLine(Squash.DataAccess.Entities.Match match)
        {
            if (match.Games == null || match.Games.Count == 0)
            {
                return null;
            }

            var games = match.Games
                .OrderBy(g => g.GameNumber)
                .Where(g => g.Side1Points.HasValue && g.Side2Points.HasValue)
                .Select(g => $"{g.Side1Points}-{g.Side2Points}")
                .ToList();

            if (games.Count == 0)
            {
                return null;
            }

            return string.Join(" ", games);
        }
    }
}
