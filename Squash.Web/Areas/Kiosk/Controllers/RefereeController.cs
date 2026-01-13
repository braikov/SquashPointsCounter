using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.Web.Areas.Kiosk.Models;

namespace Squash.Web.Areas.Kiosk.Controllers
{
    [Area("Kiosk")]
    public class RefereeController : Controller
    {
        private readonly IDataContext _dataContext;

        public RefereeController(IDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public IActionResult Index()
        {
            var pin = HttpContext.Session.GetString("MatchPin");
            if (string.IsNullOrWhiteSpace(pin))
            {
                return Redirect("/m");
            }

            var match = _dataContext.Matches
                .Include(m => m.Draw)
                .Include(m => m.Court)
                .Include(m => m.Games)!.ThenInclude(g => g.EventLogs)
                .Include(m => m.Player1)!.ThenInclude(p => p.Nationality)
                .Include(m => m.Player2)!.ThenInclude(p => p.Nationality)
                .FirstOrDefault(m => m.PinCode == pin);

            if (match == null)
            {
                return Redirect("/m");
            }

            var model = new RefereeMatchViewModel
            {
                Draw = match.Draw?.Name ?? string.Empty,
                Court = match.Court?.Name ?? string.Empty,
                FirstPlayer = MapPlayer(match.Player1),
                SecondPlayer = MapPlayer(match.Player2),
                MatchGameId = 0,
                GameScoreFirst = (byte)match.Games.Count(g => g.WinnerSide == 1),
                GameScoreSecond = (byte)match.Games.Count(g => g.WinnerSide == 2),
                CurrentGameScoreFirst = 0,
                CurrentGameScoreSecond = 0,
#warning TODO: Replace hardcoded match format once it is stored in the database.
                GamesToWin = 3
            };

            var activeGame = match.Games
                .OrderByDescending(g => g.GameNumber)
                .FirstOrDefault(g => g.WinnerSide == null);

            if (activeGame != null)
            {
                var scores = CalculateCurrentGameScore(activeGame.EventLogs);
                model.MatchGameId = activeGame.Id;
                model.CurrentGameScoreFirst = (byte)scores.first;
                model.CurrentGameScoreSecond = (byte)scores.second;
            }

            return View(model);
        }
        private static RefereePlayerViewModel MapPlayer(Squash.DataAccess.Entities.Player? player)
        {
            if (player == null)
            {
                return new RefereePlayerViewModel();
            }

            var nationalityCode = player.Nationality?.Code ?? string.Empty;
            var nationalityName = player.Nationality?.Name ?? nationalityCode;

            return new RefereePlayerViewModel
            {
                Name = player.Name ?? string.Empty,
                PictureUrl = null,
                Nationality = nationalityName ?? string.Empty,
                NationalityFlagUrl = string.IsNullOrWhiteSpace(nationalityCode)
                    ? string.Empty
                    : $"/images/flags/{nationalityCode.ToLowerInvariant()}.svg"
            };
        }

        private static (int first, int second) CalculateCurrentGameScore(IEnumerable<Squash.DataAccess.Entities.MatchGameEventLog> logs)
        {
            var first = 0;
            var second = 0;

            foreach (var log in logs)
            {
                if (!log.IsValid || !log.IsPoint)
                {
                    continue;
                }

                switch (log.Event)
                {
                    case Squash.DataAccess.Entities.MatchGameEvent.PointA:
                    case Squash.DataAccess.Entities.MatchGameEvent.StrokeA:
                    case Squash.DataAccess.Entities.MatchGameEvent.ConductStrokeA:
                        first += 1;
                        break;
                    case Squash.DataAccess.Entities.MatchGameEvent.PointB:
                    case Squash.DataAccess.Entities.MatchGameEvent.StrokeB:
                    case Squash.DataAccess.Entities.MatchGameEvent.ConductStrokeB:
                        second += 1;
                        break;
                }
            }

            return (first, second);
        }
    }
}

