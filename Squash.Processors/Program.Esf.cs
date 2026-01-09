using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Squash.DataAccess.Entities;
using Squash.DataAccess;
using Squash.Shared;
using Squash.Shared.Parsers.Esf;
using Squash.SqlServer;
using System.Globalization;

namespace Squash.Processors
{
    public partial class Program
    {
        public static void Download()
        {
            var days = new string[]{
            //"https://esf.tournamentsoftware.com/tournament/438dcb7f-f5b3-4cae-a0f3-7d21e4a08f06/matches/20251024",
            //"https://esf.tournamentsoftware.com/tournament/438dcb7f-f5b3-4cae-a0f3-7d21e4a08f06/matches/20251025",
            //"https://esf.tournamentsoftware.com/tournament/438dcb7f-f5b3-4cae-a0f3-7d21e4a08f06/matches/20251026" ,

            "https://esf.tournamentsoftware.com/tournament/e29aff47-d6af-4fd6-a5ae-4d2e918b397a/matches/20250102",
            "https://esf.tournamentsoftware.com/tournament/e29aff47-d6af-4fd6-a5ae-4d2e918b397a/matches/20250103",
            "https://esf.tournamentsoftware.com/tournament/e29aff47-d6af-4fd6-a5ae-4d2e918b397a/matches/20250104",
            "https://esf.tournamentsoftware.com/tournament/e29aff47-d6af-4fd6-a5ae-4d2e918b397a/matches/20250105",
            "https://esf.tournamentsoftware.com/tournament/e29aff47-d6af-4fd6-a5ae-4d2e918b397a/matches/20250106",
            };

            foreach (var day in days)
            {
                try
                {
                    var html = Downloader.Download(day);
                    var parser = new DayMatchesParser();

                    DayMatchesParseResult result = parser.Parse(html);

                    using var dbContext = GetDataContext();
                    SaveResult(dbContext, result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void SaveResult(IDataContext dbContext, DayMatchesParseResult result)
        {
            // Rule 1 do not set collection navigation properties. Work only with foreign keys.

            // Find tournament in database, create if not exists. Update result.Tournament.TournamentId.
            CreateOrUpdateTournament(dbContext, result.Tournament);

            // Find tournamentDay in database, create if not exists. Update result.TournamentDay.TournamentDayId.
            CreateOrUpdateTournamentDay(dbContext, result.TournamentDay, result.Tournament.Id);
            // Find nationalities in database, create if not exists. Update result.Nationalities' NationalityId.
            CreateOrUpdateNationalities(dbContext, result.Nationalities);
            // Find courts in database, create if not exists. Update result.Courts' CourtId.
            CreateOrUpdateCourts(dbContext, result.Courts, result.Tournament.Id);
            // Find draws in database, create if not exists. Update result.Draws' DrawId.
            CreateOrUpdateDraws(dbContext, result.Draws, result.Tournament.Id);
            // Find rounds in database, create if not exists. Update result.Rounds' RoundId.
            CreateOrUpdateRounds(dbContext, result.Rounds);
            // Find players in database, create if not exists. Update result.Players' PlayerId.
            CreateOrUpdatePlayers(dbContext, result.Players);
            // Find matches in database, create if not exists. Update result.Matches' MatchId.
            CreateOrUpdateMatches(dbContext, result.Matches, result.Tournament.Id, result.TournamentDay.Id);
            // Find match games in database, create if not exists. Update result.Games' MatchGameId.
            CreateOrUpdateMatchGames(dbContext, result.Games);

            static void CreateOrUpdateTournament(DataAccess.IDataContext dbContext, Tournament tournament)
            {
                var existingTournament = dbContext.Tournaments
                    .FirstOrDefault(t => t.ExternalCode == tournament.ExternalCode);

                if (existingTournament == null)
                {
                    existingTournament = new Tournament
                    {
                        ExternalCode = tournament.ExternalCode
                    };
                    dbContext.Tournaments.Add(existingTournament);
                }
                existingTournament.Name = tournament.Name;
                existingTournament.OrganizationCode = tournament.OrganizationCode;

                dbContext.SaveChanges();
                tournament.Id = existingTournament.Id;
            }

            static void CreateOrUpdateTournamentDay(IDataContext dbContext, TournamentDay tournamentDay, int tournamentId)
            {
                var existingDay = dbContext.TournamentDays
                    .FirstOrDefault(d => d.TournamentId == tournamentId && d.Date == tournamentDay.Date);

                if (existingDay == null)
                {
                    existingDay = new TournamentDay
                    {
                        TournamentId = tournamentId,
                        Date = tournamentDay.Date
                    };
                    dbContext.TournamentDays.Add(existingDay);
                }

                existingDay.LocationFilter = tournamentDay.LocationFilter;

                dbContext.SaveChanges();
                tournamentDay.Id = existingDay.Id;
                tournamentDay.TournamentId = existingDay.TournamentId;
            }

            static void CreateOrUpdateNationalities(IDataContext dbContext, IEnumerable<Nationality> nationalities)
            {
                foreach (var nationality in nationalities)
                {
                    if (string.IsNullOrWhiteSpace(nationality.Code))
                    {
                        throw new InvalidOperationException("Nationality.Code cannot be null or empty.");
                    }

                    var existingNationality = dbContext.Nationalities
                        .FirstOrDefault(n => n.Code == nationality.Code);

                    if (existingNationality == null)
                    {
                        existingNationality = new Nationality
                        {
                            Code = nationality.Code
                        };
                        dbContext.Nationalities.Add(existingNationality);
                    }

                    existingNationality.Name = nationality.Name;

                    dbContext.SaveChanges();
                    nationality.Id = existingNationality.Id;
                }
            }

            static void CreateOrUpdateCourts(IDataContext dbContext, IEnumerable<Court> courts, int tournamentId)
            {
                foreach (var court in courts)
                {
                    if (string.IsNullOrWhiteSpace(court.Name))
                    {
                        throw new InvalidOperationException("Court.Name cannot be null or empty.");
                    }

                    var existingCourt = dbContext.Courts
                        .FirstOrDefault(c => c.TournamentId == tournamentId && c.Name == court.Name);

                    if (existingCourt == null)
                    {
                        existingCourt = new Court
                        {
                            TournamentId = tournamentId,
                            Name = court.Name
                        };
                        dbContext.Courts.Add(existingCourt);
                    }

                    dbContext.SaveChanges();
                    court.Id = existingCourt.Id;
                    court.TournamentId = existingCourt.TournamentId;
                }
            }

            static void CreateOrUpdateDraws(IDataContext dbContext, IEnumerable<Draw> draws, int tournamentId)
            {
                foreach (var draw in draws)
                {
                    if (string.IsNullOrWhiteSpace(draw.Name) || !draw.ExternalDrawId.HasValue)
                    {
                        throw new InvalidOperationException("Draw.Name and Draw.ExternalDrawId are required.");
                    }

                    var existingDraw = draw.ExternalDrawId.HasValue
                        ? dbContext.Draws.FirstOrDefault(d => d.TournamentId == tournamentId && d.ExternalDrawId == draw.ExternalDrawId)
                        : dbContext.Draws.FirstOrDefault(d => d.TournamentId == tournamentId && d.Name == draw.Name);

                    if (existingDraw == null)
                    {
                        existingDraw = new Draw
                        {
                            TournamentId = tournamentId,
                            ExternalDrawId = draw.ExternalDrawId,
                            Name = draw.Name
                        };
                        dbContext.Draws.Add(existingDraw);
                    }

                    existingDraw.Name = draw.Name;
                    existingDraw.ExternalDrawId = draw.ExternalDrawId;

                    dbContext.SaveChanges();
                    draw.Id = existingDraw.Id;
                    draw.TournamentId = existingDraw.TournamentId;
                }
            }

            static void CreateOrUpdateRounds(IDataContext dbContext, IEnumerable<Round> rounds)
            {
                foreach (var round in rounds)
                {
                    var drawId = round.Draw?.Id ?? round.DrawId;
                    if (drawId == 0)
                    {
                        continue;
                    }

                    var existingRound = dbContext.Rounds
                        .FirstOrDefault(r => r.DrawId == drawId && r.Name == round.Name);

                    if (existingRound == null)
                    {
                        existingRound = new Round
                        {
                            DrawId = drawId,
                            Name = round.Name
                        };
                        dbContext.Rounds.Add(existingRound);
                    }

                    existingRound.Name = round.Name;

                    dbContext.SaveChanges();
                    round.Id = existingRound.Id;
                    round.DrawId = existingRound.DrawId;
                }
            }

            static void CreateOrUpdatePlayers(IDataContext dbContext, IEnumerable<Player> players)
            {
                foreach (var player in players)
                {
                    var existingPlayer = player.ExternalPlayerId.HasValue
                        ? dbContext.Players.FirstOrDefault(p => p.ExternalPlayerId == player.ExternalPlayerId)
                        : dbContext.Players.FirstOrDefault(p => p.Name == player.Name);

                    if (existingPlayer == null)
                    {
                        existingPlayer = new Player
                        {
                            ExternalPlayerId = player.ExternalPlayerId,
                            Name = player.Name
                        };
                        dbContext.Players.Add(existingPlayer);
                    }

                    existingPlayer.Name = player.Name;
                    existingPlayer.MemberId = player.MemberId;
                    existingPlayer.NationalityId = player.Nationality?.Id;

                    dbContext.SaveChanges();
                    player.Id = existingPlayer.Id;
                    player.NationalityId = existingPlayer.NationalityId;
                }
            }

            static void CreateOrUpdateMatches(
                IDataContext dbContext,
                IEnumerable<Match> matches,
                int tournamentId,
                int tournamentDayId)
            {
                foreach (var match in matches)
                {
                    var drawId = match.Draw?.Id ?? match.DrawId;
                    var roundId = match.Round?.Id ?? match.RoundId;
                    var courtId = match.Court?.Id ?? match.CourtId;
                    var player1Id = match.Player1?.Id ?? match.Player1Id;
                    var player2Id = match.Player2?.Id ?? match.Player2Id;
                    var winnerPlayerId = match.WinnerPlayer?.Id ?? match.WinnerPlayerId;

                    var existingMatch = dbContext.Matches.FirstOrDefault(m =>
                        m.TournamentDayId == tournamentDayId
                        && m.Player1Id == player1Id
                        && m.Player2Id == player2Id
                        && m.StartTimeText == match.StartTimeText);

                    if (existingMatch == null)
                    {
                        existingMatch = new Match
                        {
                            TournamentId = tournamentId,
                            TournamentDayId = tournamentDayId
                        };
                        dbContext.Matches.Add(existingMatch);
                    }

                    existingMatch.DrawId = drawId;
                    existingMatch.RoundId = roundId;
                    existingMatch.CourtId = courtId;
                    existingMatch.Player1Id = player1Id;
                    existingMatch.Player2Id = player2Id;
                    existingMatch.WinnerPlayerId = winnerPlayerId;
                    existingMatch.StartTime = match.StartTime;
                    existingMatch.StartTimeText = match.StartTimeText;
                    existingMatch.Status = match.Status;
                    existingMatch.HeadToHeadUrl = match.HeadToHeadUrl;

                    dbContext.SaveChanges();
                    match.Id = existingMatch.Id;
                    match.TournamentId = existingMatch.TournamentId;
                    match.TournamentDayId = existingMatch.TournamentDayId;
                }
            }

            static void CreateOrUpdateMatchGames(IDataContext dbContext, IEnumerable<MatchGame> games)
            {
                foreach (var game in games)
                {
                    var matchId = game.Match?.Id ?? game.MatchId;
                    if (matchId == 0)
                    {
                        continue;
                    }

                    var existingGame = dbContext.MatchGames
                        .FirstOrDefault(g => g.MatchId == matchId && g.GameNumber == game.GameNumber);

                    if (existingGame == null)
                    {
                        existingGame = new MatchGame
                        {
                            MatchId = matchId,
                            GameNumber = game.GameNumber
                        };
                        dbContext.MatchGames.Add(existingGame);
                    }

                    existingGame.Side1Points = game.Side1Points;
                    existingGame.Side2Points = game.Side2Points;
                    existingGame.WinnerSide = game.WinnerSide;

                    dbContext.SaveChanges();
                    game.Id = existingGame.Id;
                    game.MatchId = existingGame.MatchId;
                }
            }
        }
    }
}
