using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Squash.DataAccess;
using Squash.DataAccess.Entities;
using Squash.SqlServer;

namespace Squash.Shared.Parsers.Esf
{
    public static class Download
    {
        public static TournamentParseResult DownloadAndParseTournament(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentException("URL is required.", nameof(url));
            }

            var html = Squash.Shared.Downloader.Download(url);
            var parser = new TournamentParser();
            var result = parser.Parse(html);

            var tournamentId = ExtractTournamentId(url);
            result.Tournament.ExternalCode = tournamentId.ToString();
            result.Tournament.SourceUrls = url;
            result.Tournament.EntitySourceId = EntitySource.Esf;

            return result;
        }

        public static void StoreTournament(TournamentParseResult result, int userId)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(userId));
            }

            using var dbContext = CreateDataContext();

            var countryName = result.HostNation;
            if (string.IsNullOrWhiteSpace(countryName) && result.Venues.Count > 0)
            {
                countryName = result.Venues
                    .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v.CountryName))
                    ?.CountryName;
            }

            if (!string.IsNullOrWhiteSpace(countryName))
            {
                var country = dbContext.Nationalities
                    .FirstOrDefault(n =>
                        n.CountryName == countryName ||
                        n.Name == countryName);

                if (country == null)
                {
                    country = new Nationality
                    {
                        Code = countryName.Length > 3
                            ? countryName.Substring(0, 3).ToUpperInvariant()
                            : countryName.ToUpperInvariant(),
                        Name = countryName,
                        CountryName = countryName
                    };
                    dbContext.Nationalities.Add(country);
                    dbContext.SaveChanges();
                }
                result.Tournament.NationalityId = country.Id;
            }

            CreateOrUpdateTournament(dbContext, result.Tournament, userId);
            if (result.Venues.Count > 0)
            {
                SaveVenuesAndLinks(dbContext, result.Tournament.Id, result.Venues);
            }
        }
        public static void DownloadParseAndStoreMatches(string[] urls)
        {
            if (urls == null)
            {
                throw new ArgumentNullException(nameof(urls));
            }

            var parser = new DayMatchesParser();
            var results = new List<DayMatchesParseResult>();

            foreach (var url in urls)
            {
                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                var html = Squash.Shared.Downloader.Download(url);
                var result = parser.Parse(html);
                results.Add(result);
            }

            using var dbContext = CreateDataContext();
            foreach (var result in results)
            {
                SaveResult(dbContext, result);
            }
        }

        public static void DownloadParseAndStoreEventsAndDraws(Guid tournamentId, int tournamentDbId)
        {
            if (tournamentId == Guid.Empty)
            {
                throw new ArgumentOutOfRangeException(nameof(tournamentId));
            }

            if (tournamentDbId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tournamentDbId));
            }

            var eventsUrl = $"https://esf.tournamentsoftware.com/sport/events.aspx?id={tournamentId}";
            var drawsUrl = $"https://esf.tournamentsoftware.com/sport/draws.aspx?id={tournamentId}";

            var eventsHtml = Squash.Shared.Downloader.Download(eventsUrl);
            var drawsHtml = Squash.Shared.Downloader.Download(drawsUrl);

            var eventsParser = new EventsParser();
            var drawsParser = new DrawsParser();

            var eventsResult = eventsParser.Parse(eventsHtml);
            var drawsResult = drawsParser.Parse(drawsHtml);

            using var dbContext = CreateDataContext();
            CreateOrUpdateEvents(dbContext, eventsResult.Events, tournamentDbId);
            CreateOrUpdateDrawList(dbContext, drawsResult.Draws, tournamentDbId);
        }

        private static IDataContext CreateDataContext()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.{Environment.MachineName}.json", optional: true, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DataContext");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DataContext' is not configured.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlServer(connectionString, x => x.UseCompatibilityLevel(100));
            return new DataContext(optionsBuilder.Options);
        }

        private static void SaveVenuesAndLinks(IDataContext dbContext, int tournamentId, IEnumerable<VenueParseResult> venues)
        {
            foreach (var venueData in venues)
            {
                if (string.IsNullOrWhiteSpace(venueData.Name))
                {
                    continue;
                }

                // Find or create country
                int? countryId = null;
                if (!string.IsNullOrWhiteSpace(venueData.CountryName))
                {
                    var country = dbContext.Nationalities
                        .FirstOrDefault(n =>
                            n.CountryName == venueData.CountryName ||
                            n.Name == venueData.CountryName);

                    if (country == null)
                    {
                        country = new Nationality
                        {
                            Code = venueData.CountryName.Length > 3
                                ? venueData.CountryName.Substring(0, 3).ToUpperInvariant()
                                : venueData.CountryName.ToUpperInvariant(),
                            Name = venueData.CountryName,
                            CountryName = venueData.CountryName
                        };
                        dbContext.Nationalities.Add(country);
                        dbContext.SaveChanges();
                    }
                    countryId = country.Id;
                }

                var existingVenue = dbContext.Venues.FirstOrDefault(v => v.Name == venueData.Name);
                if (existingVenue == null)
                {
                    existingVenue = new Venue
                    {
                        Name = venueData.Name
                    };
                    existingVenue.Courts.Add(new Court { Name = "Court 1" });
                    dbContext.Venues.Add(existingVenue);
                    dbContext.SaveChanges();
                }

                // Update venue fields
                if (!string.IsNullOrWhiteSpace(venueData.Street))
                    existingVenue.Street = venueData.Street;
                if (!string.IsNullOrWhiteSpace(venueData.City))
                    existingVenue.City = venueData.City;
                if (!string.IsNullOrWhiteSpace(venueData.Zip))
                    existingVenue.Zip = venueData.Zip;
                if (!string.IsNullOrWhiteSpace(venueData.Region))
                    existingVenue.Region = venueData.Region;
                if (countryId.HasValue)
                    existingVenue.CountryId = countryId;
                if (venueData.Latitude.HasValue)
                    existingVenue.Latitude = venueData.Latitude;
                if (venueData.Longitude.HasValue)
                    existingVenue.Longitude = venueData.Longitude;
                if (!string.IsNullOrWhiteSpace(venueData.Phone))
                    existingVenue.Phone = venueData.Phone;
                if (!string.IsNullOrWhiteSpace(venueData.Email))
                    existingVenue.Email = venueData.Email;
                if (!string.IsNullOrWhiteSpace(venueData.Website))
                    existingVenue.Website = venueData.Website;

                dbContext.SaveChanges();

                var linkExists = dbContext.TournamentVenues
                    .Any(tv => tv.TournamentId == tournamentId && tv.VenueId == existingVenue.Id);
                if (!linkExists)
                {
                    dbContext.TournamentVenues.Add(new TournamentVenue
                    {
                        TournamentId = tournamentId,
                        VenueId = existingVenue.Id
                    });
                    dbContext.SaveChanges();
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
            var venueId = ResolveTournamentVenue(dbContext, result.Tournament);
            CreateOrUpdateCourts(dbContext, result.Courts, venueId);
            EnsureTournamentVenue(dbContext, result.Tournament.Id, venueId);
            EnsureTournamentCourts(dbContext, result.Tournament.Id, result.Courts);
            // Find draws in database, create if not exists. Update result.Draws' DrawId.
            CreateOrUpdateDraws(dbContext, result.Draws, result.Tournament.Id);
            // Find rounds in database, create if not exists. Update result.Rounds' RoundId.
            CreateOrUpdateRounds(dbContext, result.Rounds);
            // Find players in database, create if not exists. Update result.Players' PlayerId.
            CreateOrUpdatePlayers(dbContext, result.Players, result.Tournament.Id, result.TournamentPlayerIds);
            // Find matches in database, create if not exists. Update result.Matches' MatchId.
            CreateOrUpdateMatches(dbContext, result.Matches, result.Tournament.Id, result.TournamentDay.Id);
            // Find match games in database, create if not exists. Update result.Games' MatchGameId.
            CreateOrUpdateMatchGames(dbContext, result.Games);

            static void CreateOrUpdateTournament(IDataContext dbContext, Tournament tournament)
            {
                Download.CreateOrUpdateTournament(dbContext, tournament);
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

            static void CreateOrUpdateCourts(IDataContext dbContext, IEnumerable<Court> courts, int venueId)
            {
                foreach (var court in courts)
                {
                    if (string.IsNullOrWhiteSpace(court.Name))
                    {
                        throw new InvalidOperationException("Court.Name cannot be null or empty.");
                    }

                    var existingCourt = dbContext.Courts
                        .FirstOrDefault(c => c.VenueId == venueId && c.Name == court.Name);

                    if (existingCourt == null)
                    {
                        existingCourt = new Court
                        {
                            VenueId = venueId,
                            Name = court.Name
                        };
                        dbContext.Courts.Add(existingCourt);
                    }

                    dbContext.SaveChanges();
                    court.Id = existingCourt.Id;
                    court.VenueId = existingCourt.VenueId;
                }
            }

            static int ResolveTournamentVenue(IDataContext dbContext, Tournament tournament)
            {
                var existingVenueId = dbContext.TournamentVenues
                    .Where(tv => tv.TournamentId == tournament.Id)
                    .Select(tv => tv.VenueId)
                    .FirstOrDefault();
                if (existingVenueId != 0)
                {
                    return existingVenueId;
                }

                var name = $"{tournament.Name} - Default Venue";
                var existingVenue = dbContext.Venues
                    .FirstOrDefault(v => v.Name == name);

                if (existingVenue == null)
                {
                    existingVenue = new Venue
                    {
                        Name = name
                    };
                    existingVenue.Courts.Add(new Court { Name = "Court 1" });
                    dbContext.Venues.Add(existingVenue);
                    dbContext.SaveChanges();
                }

                return existingVenue.Id;
            }

            static void EnsureTournamentVenue(IDataContext dbContext, int tournamentId, int venueId)
            {
                var exists = dbContext.TournamentVenues
                    .Any(tv => tv.TournamentId == tournamentId && tv.VenueId == venueId);
                if (!exists)
                {
                    dbContext.TournamentVenues.Add(new TournamentVenue
                    {
                        TournamentId = tournamentId,
                        VenueId = venueId
                    });
                    dbContext.SaveChanges();
                }
            }

            static void EnsureTournamentCourts(IDataContext dbContext, int tournamentId, IEnumerable<Court> courts)
            {
                foreach (var court in courts)
                {
                    if (court.Id == 0)
                    {
                        continue;
                    }

                    var exists = dbContext.TournamentCourts
                        .Any(tc => tc.TournamentId == tournamentId && tc.CourtId == court.Id);
                    if (!exists)
                    {
                        dbContext.TournamentCourts.Add(new TournamentCourt
                        {
                            TournamentId = tournamentId,
                            CourtId = court.Id
                        });
                    }
                }
                dbContext.SaveChanges();
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
                        // Find matching event by name prefix
                        var matchingEvent = dbContext.Events
                            .Where(e => e.TournamentId == tournamentId && !string.IsNullOrEmpty(e.Name) && draw.Name.StartsWith(e.Name))
                            .OrderByDescending(e => e.Name.Length)
                            .FirstOrDefault();

                        existingDraw = new Draw
                        {
                            TournamentId = tournamentId,
                            ExternalDrawId = draw.ExternalDrawId,
                            Name = draw.Name,
                            EventId = matchingEvent?.Id
                        };
                        dbContext.Draws.Add(existingDraw);
                    }

                    existingDraw.Name = draw.Name;
                    existingDraw.ExternalDrawId = draw.ExternalDrawId;
                    
                    // Update EventId if not set
                    if (!existingDraw.EventId.HasValue)
                    {
                        var matchingEvent = dbContext.Events
                            .Where(e => e.TournamentId == tournamentId && !string.IsNullOrEmpty(e.Name) && draw.Name.StartsWith(e.Name))
                            .OrderByDescending(e => e.Name.Length)
                            .FirstOrDefault();
                        existingDraw.EventId = matchingEvent?.Id;
                    }

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

            static void CreateOrUpdatePlayers(IDataContext dbContext, IEnumerable<Player> players, int tournamentId, Dictionary<Player, int> tournamentPlayerIds)
            {
                foreach (var player in players)
                {
                    var existingPlayer = !string.IsNullOrWhiteSpace(player.EsfMemberId)
                        ? dbContext.Players.FirstOrDefault(p => p.EsfMemberId == player.EsfMemberId)
                        : dbContext.Players.FirstOrDefault(p => p.Name == player.Name);

                    if (existingPlayer == null)
                    {
                        existingPlayer = new Player
                        {
                            Name = player.Name
                        };
                        dbContext.Players.Add(existingPlayer);
                    }

                    existingPlayer.Name = player.Name;
                    existingPlayer.EsfMemberId = player.EsfMemberId;
                    existingPlayer.NationalityId = player.Nationality?.Id;

                    dbContext.SaveChanges();
                    player.Id = existingPlayer.Id;
                    player.NationalityId = existingPlayer.NationalityId;

                    // Get TournamentPlayerId from the dictionary
                    var tournamentPlayerId = tournamentPlayerIds.ContainsKey(player) ? tournamentPlayerIds[player] : (int?)null;

                    var tournamentPlayer = dbContext.TournamentPlayers
                        .FirstOrDefault(pt => pt.PlayerId == existingPlayer.Id && pt.TournamentId == tournamentId);
                    
                    if (tournamentPlayer == null)
                    {
                        tournamentPlayer = new TournamentPlayer
                        {
                            PlayerId = existingPlayer.Id,
                            TournamentId = tournamentId,
                            TournamentPlayerId = tournamentPlayerId
                        };
                        dbContext.TournamentPlayers.Add(tournamentPlayer);
                    }
                    else if (tournamentPlayerId.HasValue && !tournamentPlayer.TournamentPlayerId.HasValue)
                    {
                        tournamentPlayer.TournamentPlayerId = tournamentPlayerId;
                    }
                    
                    dbContext.SaveChanges();
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
                    existingMatch.Player1Walkover = match.Player1Walkover;
                    existingMatch.Player2Walkover = match.Player2Walkover;
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

        private static void CreateOrUpdateTournament(IDataContext dbContext, Tournament tournament, int? userIdOverride = null)
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

            if (userIdOverride.HasValue)
            {
                existingTournament.UserId = userIdOverride.Value;
            }
            if (!string.IsNullOrWhiteSpace(tournament.Name))
            {
                existingTournament.Name = tournament.Name;
            }
            if (!string.IsNullOrWhiteSpace(tournament.OrganizationCode))
            {
                existingTournament.OrganizationCode = tournament.OrganizationCode;
            }
            if (!string.IsNullOrWhiteSpace(tournament.SourceUrls))
            {
                existingTournament.SourceUrls = tournament.SourceUrls;
            }
            if (tournament.StartDate.HasValue)
            {
                existingTournament.StartDate = tournament.StartDate;
            }
            if (tournament.EndDate.HasValue)
            {
                existingTournament.EndDate = tournament.EndDate;
            }
            if (tournament.EntryOpensDate.HasValue)
            {
                existingTournament.EntryOpensDate = tournament.EntryOpensDate;
            }
            if (tournament.ClosingSigninDate.HasValue)
            {
                existingTournament.ClosingSigninDate = tournament.ClosingSigninDate;
            }
            if (tournament.WithdrawalDeadlineDate.HasValue)
            {
                existingTournament.WithdrawalDeadlineDate = tournament.WithdrawalDeadlineDate;
            }
            if (!string.IsNullOrWhiteSpace(tournament.Regulations))
            {
                existingTournament.Regulations = tournament.Regulations;
            }
            if (tournament.NationalityId > 0)
            {
                existingTournament.NationalityId = tournament.NationalityId;
            }
            existingTournament.EntitySourceId = tournament.EntitySourceId;

            dbContext.SaveChanges();
            tournament.Id = existingTournament.Id;
        }

        private static void CreateOrUpdateEvents(IDataContext dbContext, IEnumerable<Event> events, int tournamentId)
        {
            foreach (var item in events)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    continue;
                }

                var existingEvent = item.ExternalEventId.HasValue
                    ? dbContext.Events.FirstOrDefault(e => e.TournamentId == tournamentId && e.ExternalEventId == item.ExternalEventId)
                    : dbContext.Events.FirstOrDefault(e => e.TournamentId == tournamentId && e.Name == item.Name);

                if (existingEvent == null)
                {
                    existingEvent = new Event
                    {
                        TournamentId = tournamentId,
                        ExternalEventId = item.ExternalEventId,
                        Name = item.Name
                    };
                    dbContext.Events.Add(existingEvent);
                }

                existingEvent.Name = item.Name;
                existingEvent.ExternalEventId = item.ExternalEventId;
                existingEvent.MatchType = item.MatchType;
                existingEvent.Age = item.Age;
                existingEvent.Direction = item.Direction;

                dbContext.SaveChanges();
                item.Id = existingEvent.Id;
                item.TournamentId = existingEvent.TournamentId;
            }
        }

        private static void CreateOrUpdateDrawList(IDataContext dbContext, IEnumerable<Draw> draws, int tournamentId)
        {
            foreach (var draw in draws)
            {
                if (string.IsNullOrWhiteSpace(draw.Name) || !draw.ExternalDrawId.HasValue)
                {
                    continue;
                }

                var existingDraw = dbContext.Draws.FirstOrDefault(d => d.TournamentId == tournamentId && d.ExternalDrawId == draw.ExternalDrawId);
                if (existingDraw == null)
                {
                // Find matching event by name prefix
                var matchingEvent = dbContext.Events
                    .Where(e => e.TournamentId == tournamentId && !string.IsNullOrEmpty(e.Name) && draw.Name.StartsWith(e.Name))
                    .OrderByDescending(e => e.Name.Length)
                    .FirstOrDefault();

                existingDraw = new Draw
                {
                    TournamentId = tournamentId,
                    ExternalDrawId = draw.ExternalDrawId,
                    Name = draw.Name,
                    EventId = matchingEvent?.Id
                };
                dbContext.Draws.Add(existingDraw);
            }

            existingDraw.Name = draw.Name;
            existingDraw.ExternalDrawId = draw.ExternalDrawId;
            existingDraw.Type = draw.Type;
            
            // Update EventId if not set
            if (!existingDraw.EventId.HasValue)
            {
                var matchingEvent = dbContext.Events
                    .Where(e => e.TournamentId == tournamentId && !string.IsNullOrEmpty(e.Name) && draw.Name.StartsWith(e.Name))
                    .OrderByDescending(e => e.Name.Length)
                    .FirstOrDefault();
                existingDraw.EventId = matchingEvent?.Id;
            }
                draw.TournamentId = existingDraw.TournamentId;
            }
        }

        private static Guid ExtractTournamentId(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException("Invalid tournament URL.");
            }

            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length != 2 || !string.Equals(segments[0], "tournament", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid tournament URL.");
            }

            if (!Guid.TryParse(segments[1], out var id))
            {
                throw new InvalidOperationException("Invalid tournament URL.");
            }

            return id;
        }
    }
}
