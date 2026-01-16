using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.Web.Areas.Administration.Models;
using Squash.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

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
                    PlayersCount = t.TournamentPlayers.Count,
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
            var model = new TournamentEditViewModel();
            PrepareViewModel(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TournamentEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PrepareViewModel(model);
                return View(model);
            }

            var identityUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(identityUserId))
            {
                ModelState.AddModelError(string.Empty, "Cannot determine current user.");
                PrepareViewModel(model);
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
                    PrepareViewModel(model);
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
                PrepareViewModel(model);
                return View(model);
            }

            var tournament = new Squash.DataAccess.Entities.Tournament
            {
                Name = model.Name?.Trim() ?? string.Empty,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                EntryOpensDate = model.EntryOpensDate,
                ClosingSigninDate = model.ClosingSigninDate,
                WithdrawalDeadlineDate = model.WithdrawalDeadlineDate,
                Regulations = model.Regulations,
                NationalityId = model.NationalityId,
                UserId = userId,
                EntitySourceId = Squash.DataAccess.Entities.EntitySource.Native
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
                EntryOpensDate = tournament.EntryOpensDate,
                ClosingSigninDate = tournament.ClosingSigninDate,
                WithdrawalDeadlineDate = tournament.WithdrawalDeadlineDate,
                Regulations = tournament.Regulations,
                NationalityId = tournament.NationalityId
            };
            PrepareViewModel(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details(TournamentEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PrepareViewModel(model);
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
            tournament.EntryOpensDate = model.EntryOpensDate;
            tournament.ClosingSigninDate = model.ClosingSigninDate;
            tournament.WithdrawalDeadlineDate = model.WithdrawalDeadlineDate;
            tournament.Regulations = model.Regulations;
            tournament.NationalityId = model.NationalityId;

            _dataContext.SaveChanges();

            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        [HttpGet]
        public IActionResult DayMatches(int id, int? dayId, string? eventName, string? country, string? draw, string? round, string? court)
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
            var availableEvents = new List<FilterOption>();
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
                    .Include(m => m.Draw).ThenInclude(d => d.Event)
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

                if (!string.IsNullOrEmpty(eventName))
                {
                    filteredMatches = filteredMatches.Where(m => 
                        m.Draw?.Event?.Name != null && m.Draw.Event.Name.Equals(eventName, StringComparison.OrdinalIgnoreCase));
                }

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
                availableEvents = _dataContext.Events
                    .AsNoTracking()
                    .Where(e => e.TournamentId == id)
                    .OrderBy(e => e.Name)
                    .Select(e => new FilterOption { Value = e.Name, Text = e.Name })
                    .ToList();

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

                // Get TournamentPlayer IDs for all players in the matches
                var playerIds = filteredMatchesList
                    .SelectMany(m => new[] { m.Player1Id, m.Player2Id })
                    .Where(pid => pid.HasValue)
                    .Distinct()
                    .ToList();

                var tournamentPlayerIds = _dataContext.TournamentPlayers
                    .AsNoTracking()
                    .Where(tp => tp.TournamentId == id && playerIds.Contains(tp.PlayerId))
                    .ToDictionary(tp => tp.PlayerId, tp => tp.Id);

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
                        Player1TournamentPlayerId = m.Player1Id.HasValue && tournamentPlayerIds.ContainsKey(m.Player1Id.Value) ? tournamentPlayerIds[m.Player1Id.Value] : null,
                        Player2TournamentPlayerId = m.Player2Id.HasValue && tournamentPlayerIds.ContainsKey(m.Player2Id.Value) ? tournamentPlayerIds[m.Player2Id.Value] : null,
                        Player1FlagUrl = BuildFlagUrl(m.Player1?.Nationality?.Code),
                        Player2FlagUrl = BuildFlagUrl(m.Player2?.Nationality?.Code),
                        Player1CountryCode = m.Player1?.Nationality?.Code ?? string.Empty,
                        Player2CountryCode = m.Player2?.Nationality?.Code ?? string.Empty,
                        Player1IsWinner = m.WinnerPlayerId.HasValue && m.Player1Id.HasValue && m.WinnerPlayerId == m.Player1Id,
                        Player2IsWinner = m.WinnerPlayerId.HasValue && m.Player2Id.HasValue && m.WinnerPlayerId == m.Player2Id,
                        Player1Walkover = m.Player1Walkover,
                        Player2Walkover = m.Player2Walkover,
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
                FilterEvent = eventName,
                FilterCountry = country,
                FilterDraw = draw,
                FilterRound = round,
                FilterCourt = court,
                AvailableEvents = availableEvents,
                AvailableCountries = availableCountries,
                AvailableDraws = availableDraws,
                AvailableRounds = availableRounds,
                AvailableCourts = availableCourts,
                TotalMatchesCount = totalMatchesCount,
                FilteredMatchesCount = matches.Count
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Players(int id, string? eventName, string? country, string? draw, string? round, string? court)
        {
            var tournament = _dataContext.Tournaments
                .AsNoTracking()
                .FirstOrDefault(t => t.Id == id);

            if (tournament == null)
            {
                return NotFound();
            }

            var matchEntities = _dataContext.Matches
                .AsNoTracking()
                .Where(m => m.TournamentId == id)
                .Include(m => m.Draw).ThenInclude(d => d.Event)
                .Include(m => m.Round)
                .Include(m => m.Court)
                .Include(m => m.Player1)!.ThenInclude(p => p.Nationality)
                .Include(m => m.Player2)!.ThenInclude(p => p.Nationality)
                .ToList();

            var totalPlayersCount = matchEntities
                .SelectMany(m => new[] { m.Player1, m.Player2 })
                .Where(p => p != null)
                .GroupBy(p => p!.Id)
                .Count();

            var filteredMatches = matchEntities.AsEnumerable();

            if (!string.IsNullOrEmpty(eventName))
            {
                filteredMatches = filteredMatches.Where(m => 
                    m.Draw?.Event?.Name != null && m.Draw.Event.Name.Equals(eventName, StringComparison.OrdinalIgnoreCase));
            }

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

            var availableEvents = _dataContext.Events
                .AsNoTracking()
                .Where(e => e.TournamentId == id)
                .OrderBy(e => e.Name)
                .Select(e => new FilterOption { Value = e.Name, Text = e.Name })
                .ToList();

            var availableCountries = filteredMatchesList
                .SelectMany(m => new[] { m.Player1?.Nationality?.Code, m.Player2?.Nationality?.Code })
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .Select(c => new FilterOption { Value = c!, Text = c!.ToUpperInvariant() })
                .ToList();

            var availableDraws = filteredMatchesList
                .Where(m => m.Draw != null)
                .Select(m => m.Draw!.Name)
                .Distinct()
                .OrderBy(d => d)
                .Select(d => new FilterOption { Value = d, Text = d })
                .ToList();

            var availableRounds = filteredMatchesList
                .Where(m => m.Round != null)
                .Select(m => m.Round!.Name)
                .Distinct()
                .OrderBy(r => r)
                .Select(r => new FilterOption { Value = r, Text = r })
                .ToList();

            var availableCourts = filteredMatchesList
                .Where(m => m.Court != null)
                .Select(m => m.Court!.Name)
                .Distinct()
                .OrderBy(c => c)
                .Select(c => new FilterOption { Value = c, Text = c })
                .ToList();

            var playersFromMatches = filteredMatchesList
                .SelectMany(m => new[] { m.Player1, m.Player2 })
                .Where(p => p != null)
                .GroupBy(p => p!.Id)
                .Select(g => g.First()!);

            // Apply country filter to players themselves, not just to matches
            if (!string.IsNullOrEmpty(country))
            {
                playersFromMatches = playersFromMatches.Where(p =>
                    p.Nationality?.Code != null && p.Nationality.Code.Equals(country, StringComparison.OrdinalIgnoreCase));
            }

            // Get TournamentPlayer IDs for each player
            var playerIds = playersFromMatches.Select(p => p.Id).ToList();
            var tournamentPlayers = _dataContext.TournamentPlayers
                .AsNoTracking()
                .Where(tp => tp.TournamentId == id && playerIds.Contains(tp.PlayerId))
                .ToDictionary(tp => tp.PlayerId, tp => tp.Id);

            var playerItems = playersFromMatches
                .OrderBy(p => p.Name)
                .Select(p => new TournamentPlayerItemViewModel
                {
                    Id = tournamentPlayers.ContainsKey(p.Id) ? tournamentPlayers[p.Id] : 0,
                    Name = p.Name,
                    CountryCode = p.Nationality?.Code ?? string.Empty,
                    FlagUrl = BuildFlagUrl(p.Nationality?.Code)
                })
                .Where(p => p.Id > 0) // Only include players that have a TournamentPlayer record
                .ToList();

            var playerGroups = playerItems
                .GroupBy(p => GetPlayerLetter(p.Name))
                .OrderBy(g => g.Key)
                .Select(g => new TournamentPlayerGroupViewModel
                {
                    Letter = g.Key,
                    Players = g.ToList()
                })
                .ToList();

            var model = new TournamentPlayersViewModel
            {
                TournamentId = tournament.Id,
                TournamentName = tournament.Name,
                PlayerGroups = playerGroups,
                FilterEvent = eventName,
                FilterCountry = country,
                FilterDraw = draw,
                FilterRound = round,
                FilterCourt = court,
                AvailableEvents = availableEvents,
                AvailableCountries = availableCountries,
                AvailableDraws = availableDraws,
                AvailableRounds = availableRounds,
                AvailableCourts = availableCourts,
                TotalPlayersCount = totalPlayersCount,
                FilteredPlayersCount = playerItems.Count
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Events(int id, int? eventId)
        {
            var tournament = _dataContext.Tournaments
                .AsNoTracking()
                .FirstOrDefault(t => t.Id == id);

            if (tournament == null)
            {
                return NotFound();
            }

            var form = new TournamentEventEditViewModel
            {
                TournamentId = id,
                UsePresetAge = true
            };

            if (eventId.HasValue)
            {
                var existing = _dataContext.Events
                    .AsNoTracking()
                    .FirstOrDefault(e => e.TournamentId == id && e.Id == eventId.Value);
                if (existing != null)
                {
                    form.Id = existing.Id;
                    form.Name = existing.Name;
                    form.MatchType = existing.MatchType;

                    if (TryGetAgePreset(existing.Age, existing.Direction, out var preset))
                    {
                        form.UsePresetAge = true;
                        form.AgePreset = preset;
                    }
                    else
                    {
                        form.UsePresetAge = false;
                        form.CustomAge = existing.Age;
                        form.Direction = existing.Direction;
                    }
                }
            }

            var model = BuildEventsViewModel(id, tournament.Name, form);
            return View(model);
        }

        [HttpGet]
        public IActionResult Draws(int id, int? eventId)
        {
            var tournament = _dataContext.Tournaments
                .AsNoTracking()
                .FirstOrDefault(t => t.Id == id);

            if (tournament == null)
            {
                return NotFound();
            }

            string? eventName = null;
            if (eventId.HasValue)
            {
                eventName = _dataContext.Events
                    .AsNoTracking()
                    .Where(e => e.TournamentId == id && e.Id == eventId.Value)
                    .Select(e => e.Name)
                    .FirstOrDefault();
            }

            var drawsQuery = _dataContext.Draws
                .AsNoTracking()
                .Where(d => d.TournamentId == id);

            if (!string.IsNullOrWhiteSpace(eventName))
            {
                var prefix = eventName.ToLowerInvariant();
                drawsQuery = drawsQuery.Where(d => d.Name.ToLower().StartsWith(prefix));
            }

            var draws = drawsQuery
                .OrderBy(d => d.Name)
                .Select(d => new TournamentDrawListItemViewModel
                {
                    Id = d.Id,
                    Name = d.Name,
                    Type = d.Type
                })
                .ToList();

            var entries = BuildEntriesList(id, eventName);

            var model = new TournamentDrawsViewModel
            {
                TournamentId = id,
                TournamentName = tournament.Name,
                EventName = eventName,
                Draws = draws,
                Entries = entries
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveEvent([Bind(Prefix = "EventForm")] TournamentEventEditViewModel model)
        {
            var tournament = _dataContext.Tournaments
                .AsNoTracking()
                .FirstOrDefault(t => t.Id == model.TournamentId);
            if (tournament == null)
            {
                return NotFound();
            }

            if (!TryResolveAge(model, out var age, out var direction))
            {
                var invalidModel = BuildEventsViewModel(model.TournamentId, tournament.Name, model);
                return View("Events", invalidModel);
            }

            if (!ModelState.IsValid)
            {
                var invalidModel = BuildEventsViewModel(model.TournamentId, tournament.Name, model);
                return View("Events", invalidModel);
            }

            Event? entity = null;
            if (model.Id.HasValue)
            {
                entity = _dataContext.Events.FirstOrDefault(e => e.Id == model.Id.Value && e.TournamentId == model.TournamentId);
            }

            if (entity == null)
            {
                entity = new Event
                {
                    TournamentId = model.TournamentId
                };
                _dataContext.Events.Add(entity);
            }

            entity.Name = model.Name?.Trim() ?? string.Empty;
            entity.MatchType = model.MatchType;
            entity.Age = age;
            entity.Direction = direction;

            _dataContext.SaveChanges();

            return RedirectToAction(nameof(Events), new { id = model.TournamentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteEvent(int id, int eventId)
        {
            var entity = _dataContext.Events
                .FirstOrDefault(e => e.TournamentId == id && e.Id == eventId);
            if (entity != null)
            {
                _dataContext.Events.Remove(entity);
                _dataContext.SaveChanges();
            }

            return RedirectToAction(nameof(Events), new { id });
        }

        private static string BuildFlagUrl(string? nationalityCode)
        {
            if (string.IsNullOrWhiteSpace(nationalityCode))
            {
                return string.Empty;
            }

            return $"/images/flags/{nationalityCode.ToLowerInvariant()}.svg";
        }

        private static string GetPlayerLetter(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "#";
            }

            var trimmed = name.Trim();
            if (trimmed.Length == 0)
            {
                return "#";
            }

            var letter = trimmed[0];
            return char.IsLetter(letter) ? char.ToUpperInvariant(letter).ToString() : "#";
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
                Squash.Shared.Parsers.Esf.Download.DownloadParseAndStoreEventsAndDraws(tournamentId, parseResult.Tournament.Id);

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

        private void PrepareViewModel(TournamentEditViewModel model)
        {
            model.Nationalities = _dataContext.Nationalities
                .AsNoTracking()
                .OrderBy(n => n.CountryName)
                .Select(n => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = n.Id.ToString(),
                    Text = n.CountryName
                })
                .ToList();
        }

        private TournamentEventsViewModel BuildEventsViewModel(int tournamentId, string tournamentName, TournamentEventEditViewModel form)
        {
            var events = _dataContext.Events
                .AsNoTracking()
                .Where(e => e.TournamentId == tournamentId)
                .OrderBy(e => e.Name)
                .Select(e => new TournamentEventListItemViewModel
                {
                    Id = e.Id,
                    Name = e.Name,
                    MatchType = e.MatchType,
                    Age = e.Age,
                    Direction = e.Direction
                })
                .ToList();

            form.TournamentId = tournamentId;

            return new TournamentEventsViewModel
            {
                TournamentId = tournamentId,
                TournamentName = tournamentName,
                Events = events,
                EventForm = form
            };
        }

        private List<TournamentEntryListItemViewModel> BuildEntriesList(int tournamentId, string? eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return new List<TournamentEntryListItemViewModel>();
            }

            var prefix = eventName.ToLowerInvariant();
            var drawNames = _dataContext.Draws
                .AsNoTracking()
                .Where(d => d.TournamentId == tournamentId && d.Name.ToLower().StartsWith(prefix))
                .Select(d => d.Name)
                .ToList();

            if (drawNames.Count == 0)
            {
                return new List<TournamentEntryListItemViewModel>();
            }

            return _dataContext.Matches
                .AsNoTracking()
                .Where(m => m.TournamentId == tournamentId
                    && m.Draw != null
                    && drawNames.Contains(m.Draw.Name))
                .Select(m => m.Player1)
                .Concat(_dataContext.Matches
                    .AsNoTracking()
                    .Where(m => m.TournamentId == tournamentId
                        && m.Draw != null
                        && drawNames.Contains(m.Draw.Name))
                    .Select(m => m.Player2))
                .Where(p => p != null)
                .Select(p => new
                {
                    Name = p!.Name,
                    CountryCode = p.Nationality != null ? p.Nationality.Code : null
                })
                .Distinct()
                .OrderBy(entry => entry.Name)
                .Select(entry => new TournamentEntryListItemViewModel
                {
                    Name = entry.Name,
                    CountryCode = entry.CountryCode
                })
                .ToList();
        }

        private static bool TryResolveAge(TournamentEventEditViewModel model, out int age, out Direction direction)
        {
            age = 0;
            direction = Direction.Under;

            if (model.UsePresetAge)
            {
                if (!model.AgePreset.HasValue)
                {
                    return false;
                }

                return TryParseAgePreset(model.AgePreset.Value, out age, out direction);
            }

            if (!model.CustomAge.HasValue || model.CustomAge.Value <= 0)
            {
                return false;
            }

            if (!model.Direction.HasValue)
            {
                return false;
            }

            age = model.CustomAge.Value;
            direction = model.Direction.Value;
            return true;
        }

        private static bool TryGetAgePreset(int age, Direction direction, out EventAge preset)
        {
            foreach (var value in Enum.GetValues<EventAge>())
            {
                if (TryParseAgePreset(value, out var presetAge, out var presetDirection)
                    && presetAge == age
                    && presetDirection == direction)
                {
                    preset = value;
                    return true;
                }
            }

            preset = default;
            return false;
        }

        private static bool TryParseAgePreset(EventAge preset, out int age, out Direction direction)
        {
            age = 0;
            direction = Direction.Under;

            var name = preset.ToString();
            if (name.StartsWith("Under", StringComparison.OrdinalIgnoreCase))
            {
                direction = Direction.Under;
                return int.TryParse(name.Substring("Under".Length), out age);
            }

            if (name.StartsWith("Above", StringComparison.OrdinalIgnoreCase))
            {
                direction = Direction.Above;
                return int.TryParse(name.Substring("Above".Length), out age);
            }

            return false;
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
        [HttpGet]
        public IActionResult Venues(int id)
        {
            var tournament = _dataContext.Tournaments
                .AsNoTracking()
                .Include(t => t.TournamentVenues).ThenInclude(tv => tv.Venue).ThenInclude(v => v.Courts)
                .Include(t => t.TournamentCourts)
                .Include(t => t.Nationality)
                .FirstOrDefault(t => t.Id == id);

            if (tournament == null)
            {
                return NotFound();
            }

            var assignedVenueIds = tournament.TournamentVenues.Select(tv => tv.VenueId).ToList();
            var countryId = tournament.NationalityId;

            // Available venues: same country (if set), not already assigned
            // Note: If tournament has no nationality? User said "only those that are not already added to tournament in this country"
            // Assuming tournament.NationalityId determines the country.
            
            var availableVenuesQuery = _dataContext.Venues
                .AsNoTracking()
                .Where(v => !assignedVenueIds.Contains(v.Id));

            if (countryId != 0)
            {
                availableVenuesQuery = availableVenuesQuery.Where(v => v.CountryId == countryId);
            }

            var availableVenues = availableVenuesQuery
                .OrderBy(v => v.Name)
                .Select(v => new SelectListItem
                {
                    Value = v.Id.ToString(),
                    Text = v.Name + (!string.IsNullOrEmpty(v.City) ? $" ({v.City})" : "")
                })
                .ToList();

            var model = new TournamentVenuesViewModel
            {
                TournamentId = tournament.Id,
                TournamentName = tournament.Name,
                NationalityId = tournament.NationalityId,
                AvailableVenues = availableVenues,
                AssignedVenues = tournament.TournamentVenues
                    .Select(tv => new TournamentVenueItemViewModel
                    {
                        VenueId = tv.VenueId,
                        Name = tv.Venue.Name,
                        City = tv.Venue.City,
                        Courts = tv.Venue.Courts.Select(c => new TournamentCourtItemViewModel
                        {
                            CourtId = c.Id,
                            Name = c.Name,
                            IsAssignedToTournament = tournament.TournamentCourts.Any(tc => tc.CourtId == c.Id)
                        }).OrderBy(c => c.Name).ToList()
                    })
                    .OrderBy(v => v.Name)
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddVenue(TournamentVenuesViewModel model)
        {
            if (model.SelectedVenueId.HasValue)
            {
                var exists = _dataContext.TournamentVenues
                    .Any(tv => tv.TournamentId == model.TournamentId && tv.VenueId == model.SelectedVenueId.Value);

                if (!exists)
                {
                    _dataContext.TournamentVenues.Add(new TournamentVenue
                    {
                        TournamentId = model.TournamentId,
                        VenueId = model.SelectedVenueId.Value
                    });
                     // Also assign all courts of this venue by default? 
                     // User didn't specify, but usually "Add Venue" enables its courts.
                     // However, the mock shows "Add button next to drop down", and then we see courts.
                     // The user said "Add new vanue will navigate to vanue... Save ... navigate back".
                     // Let's just add the venue linkage.
                     
                    _dataContext.SaveChanges();
                }
            }
            return RedirectToAction(nameof(Venues), new { id = model.TournamentId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveVenue(int id, int venueId)
        {
            var tv = _dataContext.TournamentVenues
                .FirstOrDefault(x => x.TournamentId == id && x.VenueId == venueId);

            if (tv != null)
            {
                _dataContext.TournamentVenues.Remove(tv);
                
                // Also remove associated courts from this tournament?
                var venueCourts = _dataContext.Courts.Where(c => c.VenueId == venueId).Select(c => c.Id).ToList();
                var tcs = _dataContext.TournamentCourts
                    .Where(tc => tc.TournamentId == id && venueCourts.Contains(tc.CourtId))
                    .ToList();
                
                _dataContext.TournamentCourts.RemoveRange(tcs);
                
                _dataContext.SaveChanges();
            }
            return RedirectToAction(nameof(Venues), new { id = id });
        }

        [HttpPost]
        public IActionResult ToggleCourt(int id, int courtId, bool assign)
        {
            var current = _dataContext.TournamentCourts
                .FirstOrDefault(tc => tc.TournamentId == id && tc.CourtId == courtId);

            if (assign)
            {
                if (current == null)
                {
                    _dataContext.TournamentCourts.Add(new TournamentCourt
                    {
                        TournamentId = id,
                        CourtId = courtId
                    });
                }
            }
            else
            {
                if (current != null)
                {
                    _dataContext.TournamentCourts.Remove(current);
                }
            }
            _dataContext.SaveChanges();

            // Return OK or similar since this might be an AJAX call? 
            // "all operations... synchronically" usually means full page reload in MVC terms if not specified AJAX.
            // But "Toggle" often implies AJAX or checkbox form submission.
            // User said "synchronically (i.e delete, add vanue, add court delete court, etc)". 
            // This phrasing "synchronically" likely means "Synchronously" in the sense of HTTP request/response page reload, 
            // OR it means "don't queue it, do it now".
            // "Save in this case should navigate back..." refers to Venue creation.
            // "all operations on tournamet vanue page should happen synchronically"
            // I will implement this as a redirect back to Venues.
            
            return RedirectToAction(nameof(Venues), new { id = id });
        }

        // GET: Administration/Tournaments/TournamentPlayer/5
        public async Task<IActionResult> TournamentPlayer(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tournamentPlayer = await _dataContext.TournamentPlayers
                .Include(tp => tp.Player)
                .Include(tp => tp.Tournament)
                .FirstOrDefaultAsync(tp => tp.Id == id);

            if (tournamentPlayer == null)
            {
                return NotFound();
            }

            // Get all matches for this player in this tournament
            var allMatches = await _dataContext.Matches
                .Include(m => m.TournamentDay)
                .Include(m => m.Round)
                .Include(m => m.Court)
                .Include(m => m.Draw)
                .Include(m => m.Player1)
                    .ThenInclude(p => p.Nationality)
                .Include(m => m.Player2)
                    .ThenInclude(p => p.Nationality)
                .Include(m => m.Games)
                .Where(m => m.TournamentId == tournamentPlayer.TournamentId &&
                           (m.Player1Id == tournamentPlayer.PlayerId || m.Player2Id == tournamentPlayer.PlayerId))
                .OrderBy(m => m.TournamentDay.Date)
                .ThenBy(m => m.StartTime)
                .ToListAsync();

            // Calculate win-loss record only for finished matches
            var finishedMatches = allMatches.Where(m => m.WinnerPlayerId.HasValue).ToList();
            var wins = finishedMatches.Count(m => m.WinnerPlayerId == tournamentPlayer.PlayerId);
            var losses = finishedMatches.Count(m => m.WinnerPlayerId != tournamentPlayer.PlayerId);

            // Get all TournamentPlayer IDs for opponents to create links
            var opponentPlayerIds = allMatches
                .SelectMany(m => new[] { m.Player1Id, m.Player2Id })
                .Where(pid => pid.HasValue && pid != tournamentPlayer.PlayerId)
                .Distinct()
                .ToList();

            var opponentTournamentPlayers = await _dataContext.TournamentPlayers
                .Where(tp => tp.TournamentId == tournamentPlayer.TournamentId && opponentPlayerIds.Contains(tp.PlayerId))
                .ToDictionaryAsync(tp => tp.PlayerId, tp => tp.Id);

            ViewBag.Matches = allMatches;
            ViewBag.Wins = wins;
            ViewBag.Losses = losses;
            ViewBag.TotalMatches = finishedMatches.Count;
            ViewBag.OpponentTournamentPlayers = opponentTournamentPlayers;

            return View(tournamentPlayer);
        }
    }
}
