using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.Web.Areas.Administration.Models;

namespace Squash.Web.Areas.Administration.Controllers
{
    [Area("Administration")]
    [Authorize]
    public class TournamentsController : Controller
    {
        private readonly IDataContext _dataContext;

        public TournamentsController(IDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public IActionResult Index()
        {
            var tournaments = _dataContext.Tournaments
                .Select(t => new TournamentListItemViewModel
                {
                    Id = t.Id,
                    ExternalCode = t.ExternalCode,
                    Name = t.Name,
                    OrganizationCode = t.OrganizationCode,
                    DaysCount = t.Days.Count,
                    DrawsCount = t.Draws.Count,
                    CourtsCount = t.Courts.Count,
                    MatchesCount = t.Matches.Count,
                    FirstDayId = t.Days
                        .OrderBy(d => d.Date)
                        .Select(d => (int?)d.Id)
                        .FirstOrDefault()
                })
                .OrderBy(t => t.Name)
                .ToList();

            return View(tournaments);
        }

        [HttpGet]
        public IActionResult Days(int id, int? dayId)
        {
            var tournament = _dataContext.Tournaments
                .AsNoTracking()
                .FirstOrDefault(t => t.Id == id);

            if (tournament == null)
            {
                return NotFound();
            }

            var days = _dataContext.TournamentDays
                .AsNoTracking()
                .Where(d => d.TournamentId == id)
                .OrderBy(d => d.Date)
                .ToList();

            var selectedDay = dayId.HasValue ? days.FirstOrDefault(d => d.Id == dayId.Value) : days.FirstOrDefault();

            var matches = new List<TournamentMatchRowViewModel>();
            if (selectedDay != null)
            {
                var matchEntities = _dataContext.Matches
                    .AsNoTracking()
                    .Where(m => m.TournamentId == id && m.TournamentDayId == selectedDay.Id)
                    .Include(m => m.Draw)
                    .Include(m => m.Round)
                    .Include(m => m.Court)
                    .Include(m => m.Player1)!.ThenInclude(p => p.Nationality)
                    .Include(m => m.Player2)!.ThenInclude(p => p.Nationality)
                    .Include(m => m.Games)
                    .OrderBy(m => m.StartTime ?? TimeSpan.MaxValue)
                    .ThenBy(m => m.StartTimeText)
                    .ToList();

                matches = matchEntities
                    .Select(m => new TournamentMatchRowViewModel
                    {
                        Time = m.StartTime.HasValue
                            ? m.StartTime.Value.ToString(@"hh\:mm")
                            : (m.StartTimeText == null || m.StartTimeText.Trim() == string.Empty ? "-" : m.StartTimeText.Trim()),
                        Draw = m.Draw == null ? string.Empty : m.Draw.Name,
                        Round = m.Round == null ? string.Empty : m.Round.Name,
                        Court = m.Court == null ? string.Empty : m.Court.Name,
                        Player1 = m.Player1 == null ? string.Empty : m.Player1.Name,
                        Player2 = m.Player2 == null ? string.Empty : m.Player2.Name,
                        Player1FlagUrl = BuildFlagUrl(m.Player1?.Nationality?.Code),
                        Player2FlagUrl = BuildFlagUrl(m.Player2?.Nationality?.Code),
                        Player1IsWinner = m.WinnerPlayerId.HasValue && m.Player1Id.HasValue && m.WinnerPlayerId == m.Player1Id,
                        Player2IsWinner = m.WinnerPlayerId.HasValue && m.Player2Id.HasValue && m.WinnerPlayerId == m.Player2Id,
                        Status = m.Status ?? string.Empty,
                        PinCode = m.PinCode ?? string.Empty,
                        IsFinished = m.WinnerPlayerId.HasValue
                            || string.Equals(m.Status, "Finished", StringComparison.OrdinalIgnoreCase),
                        Games = m.Games
                            .OrderBy(g => g.GameNumber)
                            .Select(g => new MatchGameScoreViewModel
                            {
                                GameNumber = g.GameNumber,
                                Side1Points = g.Side1Points,
                                Side2Points = g.Side2Points,
                                WinnerSide = g.WinnerSide
                            })
                            .ToList()
                    })
                    .ToList();
            }

            var model = new TournamentDayScheduleViewModel
            {
                TournamentId = tournament.Id,
                TournamentName = tournament.Name,
                SelectedDayId = selectedDay?.Id,
                SelectedDate = selectedDay?.Date,
                Days = days.Select(d => new TournamentDayTabViewModel
                {
                    Id = d.Id,
                    Date = d.Date,
                    IsSelected = selectedDay != null && d.Id == selectedDay.Id
                }).ToList(),
                Matches = matches
            };

            return View(model);
        }

        private static string BuildFlagUrl(string? nationalityCode)
        {
            if (string.IsNullOrWhiteSpace(nationalityCode))
            {
                return string.Empty;
            }

            return $"/images/flags/{nationalityCode.ToLowerInvariant()}.svg";
        }

        [HttpGet]
        public IActionResult Imports()
        {
            return View(new EsfImportsViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Imports(EsfImportsViewModel model)
        {
            var urls = (model.Urls ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var invalidUrls = urls.Where(url => !IsValidEsfDayUrl(url)).ToList();
            if (invalidUrls.Count > 0)
            {
                model.ErrorMessage = "Invalid URL(s): " + string.Join(", ", invalidUrls);
                return View(model);
            }

            if (urls.Length == 0)
            {
                model.ErrorMessage = "Please enter at least one URL.";
                return View(model);
            }

            var timeout = TimeSpan.FromMinutes(5);
            try
            {
                await Task.Run(() => Squash.Shared.Parsers.Esf.Download.DownloadParseAndStore(urls))
                    .WaitAsync(timeout);
            }
            catch (TimeoutException)
            {
                model.ErrorMessage = "Import timed out. Please try again.";
                return View(model);
            }
            catch (Exception ex)
            {
                model.ErrorMessage = ex.Message;
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        private static bool IsValidEsfDayUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(uri.Host, "esf.tournamentsoftware.com", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 4)
            {
                return false;
            }

            if (!string.Equals(segments[0], "tournament", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!Guid.TryParse(segments[1], out _))
            {
                return false;
            }

            if (!string.Equals(segments[2], "matches", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return segments[3].Length == 8 && segments[3].All(char.IsDigit);
        }
    }
}
