using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Squash.DataAccess;
using Squash.DataAccess.Entities;
using Squash.Web.Models.Referee;

namespace Squash.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReffereeController : ControllerBase
    {
        private readonly IDataContext _dataContext;

        public ReffereeController(IDataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpGet("match")]
        public ActionResult<RefereeMatchResponse> GetMatch([FromQuery] string pin)
        {
            if (string.IsNullOrWhiteSpace(pin))
            {
                return Ok(new RefereeMatchResponse { Success = false });
            }

            var normalizedPin = pin.Trim();

            var match = _dataContext.Matches
                .Include(m => m.Draw)
                .Include(m => m.Court)
                .Include(m => m.Games)!.ThenInclude(g => g.EventLogs)
                .Include(m => m.Player1)!.ThenInclude(p => p.Nationality)
                .Include(m => m.Player2)!.ThenInclude(p => p.Nationality)
                .FirstOrDefault(m => m.PinCode == normalizedPin);

            if (match == null)
            {
                return Ok(new RefereeMatchResponse { Success = false });
            }

            HttpContext.Session.SetString("MatchPin", normalizedPin);

            var response = new RefereeMatchResponse
            {
                Success = true,
                Match = new RefereeMatch
                {
                    Draw = match.Draw?.Name ?? string.Empty,
                    Court = match.Court?.Name ?? string.Empty,
                    FirstPlayer = MapPlayer(match.Player1),
                    SecondPlayer = MapPlayer(match.Player2),
                    MatchGameId = 0,
                    GameScoreFirst = match.Games.Count(g => g.WinnerSide == 1),
                    GameScoreSecond = match.Games.Count(g => g.WinnerSide == 2),
                    CurrentGameScoreFirst = 0,
                    CurrentGameScoreSecond = 0,
#warning TODO: Replace hardcoded match format once it is stored in the database.
                    GamesToWin = 3
                }
            };

            var activeGame = match.Games
                .OrderByDescending(g => g.GameNumber)
                .FirstOrDefault(g => !IsGameFinished(g));

            if (activeGame != null)
            {
                var logs = activeGame.EventLogs
                    .Where(l => l.IsValid)
                    .OrderBy(l => l.Id)
                    .ToList();

                var currentScores = CalculateCurrentGameScore(logs);

                response.Match.MatchGameId = activeGame.Id;
                response.Match.CurrentGameScoreFirst = currentScores.first;
                response.Match.CurrentGameScoreSecond = currentScores.second;
                response.Match.EventLogs = logs
                    .Where(l => l.Event != MatchGameEvent.EndGame && l.Event != MatchGameEvent.EndMatch)
                    .Select(l => new RefereeMatchEventLog
                    {
                        Id = l.Id,
                        Event = l.Event.ToString(),
                        IsPoint = l.IsPoint,
                        IsValid = l.IsValid
                    })
                    .ToList();
            }

            return Ok(response);
        }

        [HttpPost("game-log")]
        public IActionResult GameLog([FromBody] GameLogRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.EventName))
            {
                return BadRequest();
            }

            if (!Enum.TryParse<MatchGameEvent>(request.EventName, out var matchEvent))
            {
                return BadRequest();
            }

            var matchGameId = request.MatchGameId;
            if (matchGameId <= 0)
            {
                matchGameId = ResolveActiveMatchGameIdFromSession();
                if (matchGameId <= 0)
                {
                    return BadRequest();
                }
            }

            var log = new MatchGameEventLog
            {
                MatchGameId = matchGameId,
                Event = matchEvent,
                IsPoint = IsPointEvent(matchEvent),
                IsValid = true
            };

            _dataContext.MatchGameEventLogs.Add(log);

            if (log.IsPoint)
            {
                var pointLogs = _dataContext.MatchGameEventLogs
                    .AsNoTracking()
                    .Where(l => l.MatchGameId == matchGameId && l.IsValid && l.IsPoint)
                    .ToList();
                pointLogs.Add(log);
                var scores = CalculateCurrentGameScore(pointLogs);

                if (IsGameOver(scores.first, scores.second))
                {
                    var matchGame = _dataContext.MatchGames.FirstOrDefault(mg => mg.Id == matchGameId);
                    if (matchGame != null)
                    {
                        matchGame.Side1Points = scores.first;
                        matchGame.Side2Points = scores.second;
                        if (matchGame.WinnerSide == null && scores.first != scores.second)
                        {
                            matchGame.WinnerSide = scores.first > scores.second ? 1 : 2;
                        }
                    }
                }
            }

            if (matchEvent == MatchGameEvent.EndGame && request.WinnerSide.HasValue)
            {
                var matchGame = _dataContext.MatchGames
                    .FirstOrDefault(mg => mg.Id == matchGameId);
                if (matchGame != null)
                {
                    var pointLogs = _dataContext.MatchGameEventLogs
                        .AsNoTracking()
                        .Where(l => l.MatchGameId == matchGameId && l.IsValid && l.IsPoint)
                        .ToList();
                    var scores = CalculateCurrentGameScore(pointLogs);
                    matchGame.Side1Points = scores.first;
                    matchGame.Side2Points = scores.second;
                    if (matchGame.WinnerSide == null)
                    {
                        matchGame.WinnerSide = request.WinnerSide.Value;
                    }
                    if (matchGame.WinnerSide == null && scores.first != scores.second)
                    {
                        matchGame.WinnerSide = scores.first > scores.second ? 1 : 2;
                    }
                }
            }

            _dataContext.SaveChanges();

            return Ok(new { Success = true });
        }

        [HttpPost("game-started")]
        public ActionResult<GameStartedResponse> GameStarted([FromBody] GameStartedRequest request)
        {
            var pin = request?.Pin;
            if (string.IsNullOrWhiteSpace(pin))
            {
                pin = HttpContext.Session.GetString("MatchPin");
            }

            if (string.IsNullOrWhiteSpace(pin))
            {
                return Ok(new GameStartedResponse { Success = false });
            }

            var match = _dataContext.Matches
                .Include(m => m.Games)!.ThenInclude(g => g.EventLogs)
                .FirstOrDefault(m => m.PinCode == pin);

            if (match == null)
            {
                return Ok(new GameStartedResponse { Success = false });
            }

            var activeGame = match.Games
                .OrderByDescending(g => g.GameNumber)
                .FirstOrDefault(g => !IsGameFinished(g));

            if (activeGame != null)
            {
                return Ok(new GameStartedResponse { Success = true, MatchGameId = activeGame.Id });
            }

            var nextGameNumber = match.Games.Count == 0 ? 1 : match.Games.Max(g => g.GameNumber) + 1;
            var newGame = new MatchGame
            {
                MatchId = match.Id,
                GameNumber = nextGameNumber
            };

            _dataContext.MatchGames.Add(newGame);
            _dataContext.SaveChanges();

            return Ok(new GameStartedResponse { Success = true, MatchGameId = newGame.Id });
        }

        private static RefereePlayer MapPlayer(Squash.DataAccess.Entities.Player? player)
        {
            if (player == null)
            {
                return new RefereePlayer();
            }

            var nationalityCode = player.Nationality?.Code ?? string.Empty;
            var nationalityName = player.Nationality?.Name ?? nationalityCode;

            return new RefereePlayer
            {
                Name = player.Name ?? string.Empty,
                PictureUrl = null,
                Nationality = nationalityName ?? string.Empty,
                NationalityFlagUrl = string.IsNullOrWhiteSpace(nationalityCode)
                    ? string.Empty
                    : $"/images/flags/{nationalityCode.ToLowerInvariant()}.svg"
            };
        }

        private int ResolveActiveMatchGameIdFromSession()
        {
            var pin = HttpContext.Session.GetString("MatchPin");
            if (string.IsNullOrWhiteSpace(pin))
            {
                return 0;
            }

            var match = _dataContext.Matches
                .Include(m => m.Games)!.ThenInclude(g => g.EventLogs)
                .FirstOrDefault(m => m.PinCode == pin);

            if (match == null)
            {
                return 0;
            }

            var activeGame = match.Games
                .OrderByDescending(g => g.GameNumber)
                .FirstOrDefault(g => !IsGameFinished(g));

            return activeGame?.Id ?? 0;
        }

        private static bool IsPointEvent(MatchGameEvent matchEvent)
        {
            return matchEvent == MatchGameEvent.PointA
                || matchEvent == MatchGameEvent.PointB
                || matchEvent == MatchGameEvent.StrokeA
                || matchEvent == MatchGameEvent.StrokeB
                || matchEvent == MatchGameEvent.ConductStrokeA
                || matchEvent == MatchGameEvent.ConductStrokeB;
        }

        private static (int first, int second) CalculateCurrentGameScore(IReadOnlyCollection<MatchGameEventLog> logs)
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
                    case MatchGameEvent.PointA:
                    case MatchGameEvent.StrokeA:
                    case MatchGameEvent.ConductStrokeA:
                        first += 1;
                        break;
                    case MatchGameEvent.PointB:
                    case MatchGameEvent.StrokeB:
                    case MatchGameEvent.ConductStrokeB:
                        second += 1;
                        break;
                }
            }

            return (first, second);
        }

        private static bool IsGameFinished(MatchGame game)
        {
            if (game.WinnerSide.HasValue)
            {
                return true;
            }

            return game.EventLogs.Any(log => log.IsValid && log.Event == MatchGameEvent.EndGame);
        }

        private static bool IsGameOver(int first, int second)
        {
            var maxScore = Math.Max(first, second);
            var diff = Math.Abs(first - second);
            return maxScore >= 11 && diff >= 2;
        }
    }
}
