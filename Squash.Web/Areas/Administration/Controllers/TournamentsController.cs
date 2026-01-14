using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.Web.Areas.Administration.Models;

namespace Squash.Web.Areas.Administration.Controllers
{
    [Area("Administration")]
    [Authorize]
    public class TournamentsController(IDataContext dataContext, UserManager<IdentityUser> userManager) : Controller
    {
        private readonly IDataContext _dataContext = dataContext;
        private readonly UserManager<IdentityUser> _userManager = userManager;

        public IActionResult Index(string? status, int page = 1)
        {
            const int pageSize = 20;
            var today = DateTime.Today;

            var query = _dataContext.Tournaments.AsQueryable();

            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                query = status.ToLower() switch
                {
                    "upcoming" => query.Where(t => t.StartDate > today),
                    "active" => query.Where(t => t.StartDate <= today && t.EndDate >= today),
                    "past" => query.Where(t => t.EndDate < today),
                    _ => query
                };
            }

            var totalCount = query.Count();

            var tournaments = query
                .Select(t => new TournamentListItemViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    DaysCount = t.Days.Count,
                    DrawsCount = t.Draws.Count,
                    CourtsCount = t.TournamentCourts.Count,
                    MatchesCount = t.Matches.Count,
                    FirstDayId = t.Days
                        .OrderBy(d => d.Date)
                        .Select(d => (int?)d.Id)
                        .FirstOrDefault()
                })
                .OrderByDescending(t => t.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new TournamentsIndexViewModel
            {
                Tournaments = tournaments,
                FilterStatus = status,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new TournamentEditViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TournamentEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var identityUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                ModelState.AddModelError(string.Empty, "Cannot determine current user.");
                return View(model);
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
                    ModelState.AddModelError(string.Empty, "Cannot load current user.");
                    return View(model);
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
            if (userId == 0)
            {
                ModelState.AddModelError(string.Empty, "Cannot determine current user.");
                return View(model);
            }

            var tournament = new Squash.DataAccess.Entities.Tournament
            {
                Name = model.Name?.Trim() ?? string.Empty,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                ClosingSigninDate = model.ClosingSigninDate,
                Regulations = model.Regulations,
                UserId = userId,
                TournamentSource = Squash.DataAccess.Entities.TournamentSource.Native
            };

            _dataContext.Tournaments.Add(tournament);
            _dataContext.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Details(int id)
        {
            var tournament = _dataContext.Tournaments
                .AsNoTracking()
                .FirstOrDefault(t => t.Id == id);

            if (tournament == null)
            {
                return NotFound();
            }

            var model = new TournamentEditViewModel
            {
                Id = tournament.Id,
                Name = tournament.Name,
                StartDate = tournament.StartDate,
                EndDate = tournament.EndDate,
                ClosingSigninDate = tournament.ClosingSigninDate,
                Regulations = tournament.Regulations
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details(TournamentEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var tournament = _dataContext.Tournaments
                .FirstOrDefault(t => t.Id == model.Id);

            if (tournament == null)
            {
                return NotFound();
            }

            tournament.Name = model.Name?.Trim() ?? string.Empty;
            tournament.StartDate = model.StartDate;
            tournament.EndDate = model.EndDate;
            tournament.ClosingSigninDate = model.ClosingSigninDate;
            tournament.Regulations = model.Regulations;

            _dataContext.SaveChanges();

            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpGet]
        public IActionResult DayMatches(int id, int? dayId, string? country, string? draw, string? round, string? court)
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
            var availableCountries = new List<FilterOption>();
            var availableDraws = new List<FilterOption>();
            var availableRounds = new List<FilterOption>();
            var availableCourts = new List<FilterOption>();
            int totalMatchesCount = 0;

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

                totalMatchesCount = matchEntities.Count;

                // Apply filters
                var filteredMatches = matchEntities.AsEnumerable();

                if (!string.IsNullOrEmpty(country))
                {
                    filteredMatches = filteredMatches.Where(m =>
                        (m.Player1?.Nationality?.Code != null && m.Player1.Nationality.Code.Equals(country, StringComparison.OrdinalIgnoreCase)) ||
                        (m.Player2?.Nationality?.Code != null && m.Player2.Nationality.Code.Equals(country, StringComparison.OrdinalIgnoreCase)));
                }

                if (!string.IsNullOrEmpty(draw))
                {
                    filteredMatches = filteredMatches.Where(m => m.Draw != null && m.Draw.Name.Equals(draw, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(round))
                {
                    filteredMatches = filteredMatches.Where(m => m.Round != null && m.Round.Name.Equals(round, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrEmpty(court))
                {
                    filteredMatches = filteredMatches.Where(m => m.Court != null && m.Court.Name.Equals(court, StringComparison.OrdinalIgnoreCase));
                }

                var filteredMatchesList = filteredMatches.ToList();

                // Build available filter options from filtered matches
                availableCountries = filteredMatchesList
                    .SelectMany(m => new[] { m.Player1?.Nationality?.Code, m.Player2?.Nationality?.Code })
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .OrderBy(c => c)
                    .Select(c => new FilterOption { Value = c!, Text = c!.ToUpperInvariant() })
                    .ToList();

                availableDraws = filteredMatchesList
                    .Where(m => m.Draw != null)
                    .Select(m => m.Draw!.Name)
                    .Distinct()
                    .OrderBy(d => d)
                    .Select(d => new FilterOption { Value = d, Text = d })
                    .ToList();

                availableRounds = filteredMatchesList
                    .Where(m => m.Round != null)
                    .Select(m => m.Round!.Name)
                    .Distinct()
                    .OrderBy(r => r)
                    .Select(r => new FilterOption { Value = r, Text = r })
                    .ToList();

                availableCourts = filteredMatchesList
                    .Where(m => m.Court != null)
                    .Select(m => m.Court!.Name)
                    .Distinct()
                    .OrderBy(c => c)
                    .Select(c => new FilterOption { Value = c, Text = c })
                    .ToList();

                matches = filteredMatchesList
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
                        Player1CountryCode = m.Player1?.Nationality?.Code ?? string.Empty,
                        Player2CountryCode = m.Player2?.Nationality?.Code ?? string.Empty,
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
                Matches = matches,
                FilterCountry = country,
                FilterDraw = draw,
                FilterRound = round,
                FilterCourt = court,
                AvailableCountries = availableCountries,
                AvailableDraws = availableDraws,
                AvailableRounds = availableRounds,
                AvailableCourts = availableCourts,
                TotalMatchesCount = totalMatchesCount,
                FilteredMatchesCount = matches.Count
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
            var tournamentUrl = model.TournamentUrl?.Trim() ?? string.Empty;
            if (!IsValidEsfTournamentUrl(tournamentUrl, out var tournamentId))
            {
                model.ErrorMessage = "Invalid tournament URL.";
                return View(model);
            }

            var timeout = TimeSpan.FromMinutes(5);
            try
            {
                var parseResult = await Task.Run(() => Squash.Shared.Parsers.Esf.Download.DownloadAndParseTournament(tournamentUrl))
                    .WaitAsync(timeout);

                if (parseResult.Tournament.StartDate == null || parseResult.Tournament.EndDate == null)
                {
                    model.ErrorMessage = "Tournament dates are missing in the source.";
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(parseResult.ContactEmail))
                {
                    model.ErrorMessage = "Tournament contact email is missing in the source.";
                    return View(model);
                }

                var identityUser = await _userManager.FindByEmailAsync(parseResult.ContactEmail);
                if (identityUser == null)
                {
                    identityUser = new IdentityUser
                    {
                        UserName = parseResult.ContactEmail,
                        Email = parseResult.ContactEmail,
                        EmailConfirmed = false
                    };

                    var identityResult = await _userManager.CreateAsync(identityUser);
                    if (!identityResult.Succeeded)
                    {
                        model.ErrorMessage = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                        return View(model);
                    }
                }

                var appUser = _dataContext.Users.FirstOrDefault(u => u.IdentityUserId == identityUser.Id);
                if (appUser == null)
                {
                    appUser = new Squash.DataAccess.Entities.User
                    {
                        IdentityUserId = identityUser.Id,
                        Name = string.IsNullOrWhiteSpace(parseResult.ContactName) ? parseResult.ContactEmail : parseResult.ContactName,
                        Email = parseResult.ContactEmail,
                        Phone = string.IsNullOrWhiteSpace(parseResult.ContactPhone) ? "N/A" : parseResult.ContactPhone,
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
                    _dataContext.Users.Add(appUser);
                    _dataContext.SaveChanges();
                }

                Squash.Shared.Parsers.Esf.Download.StoreTournament(parseResult, appUser.Id);

                var start = parseResult.Tournament.StartDate.Value.Date;
                var end = parseResult.Tournament.EndDate.Value.Date;
                if (end < start)
                {
                    model.ErrorMessage = "Tournament end date is before start date.";
                    return View(model);
                }

                var urls = new List<string>();
                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    urls.Add($"https://esf.tournamentsoftware.com/tournament/{tournamentId}/matches/{date:yyyyMMdd}");
                }

                await Task.Run(() => Squash.Shared.Parsers.Esf.Download.DownloadParseAndStoreMatches(urls.ToArray()))
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

        private static bool IsValidEsfTournamentUrl(string url, out Guid tournamentId)
        {
            tournamentId = Guid.Empty;
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
            if (segments.Length != 2)
            {
                return false;
            }

            if (!string.Equals(segments[0], "tournament", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return Guid.TryParse(segments[1], out tournamentId);
        }
    }
}
