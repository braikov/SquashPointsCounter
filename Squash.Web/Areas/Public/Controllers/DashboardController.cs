using System.IO;
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
    public class DashboardController(
        IDataContext dataContext,
        UserManager<IdentityUser> userManager,
        IWebHostEnvironment webHostEnvironment) : Controller
    {
        private readonly IDataContext _dataContext = dataContext;
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

        [HttpGet]
        [Route("{culture:regex(^bg|en$)}/dashboard")]
        public IActionResult Index(string culture)
        {
            var appUser = GetCurrentAppUser();
            if (appUser == null)
            {
                return Forbid();
            }

            var player = appUser.Player;
            var country = appUser.Country ?? player?.Country;
            var countryName = country?.CountryName ?? country?.Nationality ?? country?.Code;
            var flagUrl = ResolveFlagUrl(country?.Code);

            var matches = GetPlayerMatches(player?.Id);
            var stats = BuildStats(player?.Id, matches);

            var model = new DashboardViewModel
            {
                Title = "Dashboard",
                PlayerName = string.IsNullOrWhiteSpace(appUser.Name)
                    ? $"{appUser.FirstName} {appUser.LastName}".Trim()
                    : appUser.Name,
                Initials = BuildInitials(appUser.Name, appUser.FirstName, appUser.LastName),
                ImaId = player?.ImaId,
                EsfId = player?.EsfMemberId,
                RankedinId = player?.RankedinId,
                CountryName = countryName,
                CountryFlagUrl = flagUrl,
                Age = CalculateAge(appUser.BirthDate),
                PictureUrl = player?.PictureUrl,
                Stats = stats,
                RecentMatches = BuildRecentMatches(player?.Id, matches)
            };
            return View(model);
        }

        private Squash.DataAccess.Entities.User? GetCurrentAppUser()
        {
            var identityUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                return null;
            }

            return _dataContext.Users
                .Include(u => u.Country)
                .Include(u => u.Player)
                    .ThenInclude(p => p!.Country)
                .FirstOrDefault(u => u.IdentityUserId == identityUserId);
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

        private static int? CalculateAge(DateTime birthDate)
        {
            if (birthDate == default)
            {
                return null;
            }

            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }

            return age < 0 ? null : age;
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

        private List<Match> GetPlayerMatches(int? playerId)
        {
            if (!playerId.HasValue)
            {
                return new List<Match>();
            }

            return _dataContext.Matches
                .AsNoTracking()
                .Include(m => m.Player1)
                .Include(m => m.Player2)
                .Include(m => m.Tournament)
                .Include(m => m.TournamentDay)
                .Include(m => m.Games)
                .Where(m => m.Player1Id == playerId.Value || m.Player2Id == playerId.Value)
                .ToList();
        }

        private static DashboardStatsViewModel BuildStats(int? playerId, List<Match> matches)
        {
            if (!playerId.HasValue)
            {
                return new DashboardStatsViewModel();
            }

            var completed = matches.Where(m => m.WinnerPlayerId.HasValue).ToList();
            var careerWins = completed.Count(m => m.WinnerPlayerId == playerId.Value);
            var careerLosses = completed.Count - careerWins;

            var year = DateTime.UtcNow.Year;
            var yearCompleted = completed.Where(m => GetMatchDate(m)?.Year == year).ToList();
            var yearWins = yearCompleted.Count(m => m.WinnerPlayerId == playerId.Value);
            var yearLosses = yearCompleted.Count - yearWins;

            var form = completed
                .OrderByDescending(m => GetMatchDate(m) ?? DateTime.MinValue)
                .Take(5)
                .Select(m => m.WinnerPlayerId == playerId.Value ? "W" : "L")
                .ToList();

            return new DashboardStatsViewModel
            {
                CareerWins = careerWins,
                CareerLosses = careerLosses,
                YearWins = yearWins,
                YearLosses = yearLosses,
                FormResults = form
            };
        }

        private static List<DashboardMatchViewModel> BuildRecentMatches(int? playerId, List<Match> matches)
        {
            if (!playerId.HasValue)
            {
                return new List<DashboardMatchViewModel>();
            }

            return matches
                .OrderByDescending(m => GetMatchDate(m) ?? DateTime.MinValue)
                .Take(3)
                .Select(m => new DashboardMatchViewModel
                {
                    OpponentName = GetOpponentName(m, playerId.Value),
                    TournamentName = m.Tournament?.Name ?? string.Empty,
                    MatchDate = GetMatchDate(m),
                    ResultLabel = GetResultLabel(m, playerId.Value),
                    Score = GetMatchScore(m, playerId.Value)
                })
                .ToList();
        }

        private static string GetOpponentName(Match match, int playerId)
        {
            if (match.Player1Id == playerId)
            {
                return match.Player2?.Name ?? "TBD";
            }

            if (match.Player2Id == playerId)
            {
                return match.Player1?.Name ?? "TBD";
            }

            return "TBD";
        }

        private static string GetResultLabel(Match match, int playerId)
        {
            if (!match.WinnerPlayerId.HasValue)
            {
                return "-";
            }

            return match.WinnerPlayerId == playerId ? "W" : "L";
        }

        private static string? GetMatchScore(Match match, int playerId)
        {
            if (match.Games == null || match.Games.Count == 0)
            {
                return null;
            }

            var mySide = match.Player1Id == playerId ? 1 : 2;
            var opponentSide = mySide == 1 ? 2 : 1;
            var myGames = match.Games.Count(g => g.WinnerSide == mySide);
            var opponentGames = match.Games.Count(g => g.WinnerSide == opponentSide);

            if (myGames == 0 && opponentGames == 0)
            {
                return null;
            }

            return $"{myGames}-{opponentGames}";
        }

        private static DateTime? GetMatchDate(Match match)
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
    }
}
