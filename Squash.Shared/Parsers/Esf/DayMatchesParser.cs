using HtmlAgilityPack;
using Squash.DataAccess.Entities;
using System.Globalization;
using System.Text.Json;

namespace Squash.Shared.Parsers.Esf
{
    public class DayMatchesParser
    {
        public DayMatchesParseResult Parse(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new ArgumentException("HTML content is empty.", nameof(html));
            }

            var normalizedHtml = NormalizeHtml(html);
            var doc = new HtmlDocument();
            doc.LoadHtml(normalizedHtml);

            var tournamentName = ExtractTournamentName(doc);
            var (tournamentCode, dayDate, locationFilter) = ExtractMatchesInit(doc);

            if (string.IsNullOrWhiteSpace(tournamentCode) && string.IsNullOrWhiteSpace(tournamentName))
            {
                throw new InvalidOperationException("Tournament metadata is missing in the matches page.");
            }

            var tournament = new Tournament
            {
                ExternalCode = tournamentCode ?? string.Empty,
                Name = tournamentName ?? string.Empty,
                EntitySourceId = EntitySource.Esf
            };

            var tournamentDay = new TournamentDay
            {
                Date = dayDate,
                LocationFilter = locationFilter
            };

            tournamentDay.Tournament = tournament;
            tournament.Days.Add(tournamentDay);

            var result = new DayMatchesParseResult(tournament, tournamentDay);

            var drawsByKey = new Dictionary<string, Draw>(StringComparer.OrdinalIgnoreCase);
            var roundsByKey = new Dictionary<string, Round>(StringComparer.OrdinalIgnoreCase);
            var courtsByKey = new Dictionary<string, Court>(StringComparer.OrdinalIgnoreCase);
            var nationalitiesByCode = new Dictionary<string, Country>(StringComparer.OrdinalIgnoreCase);
            var playersByKey = new Dictionary<string, Player>(StringComparer.OrdinalIgnoreCase);

            var matchNodes = doc.DocumentNode.SelectNodes("//div[contains(concat(' ', normalize-space(@class), ' '), ' match--list ')]");
            if (matchNodes == null)
            {
                // No matches found - this is normal for future dates
                return result;
            }

            foreach (var matchNode in matchNodes)
            {
                var match = new Match
                {
                    Tournament = result.Tournament,
                    TournamentDay = result.TournamentDay
                };

                var startTimeText = ExtractStartTimeText(matchNode);
                match.StartTimeText = startTimeText;
                if (TimeSpan.TryParse(startTimeText, out var startTime))
                {
                    match.StartTime = startTime;
                }

                var (drawName, drawId, roundName) = ExtractDrawAndRound(matchNode);
                if (string.IsNullOrWhiteSpace(drawName) || !drawId.HasValue)
                {
                    throw new InvalidOperationException("Missing draw name or draw id for match.");
                }

                if (!string.IsNullOrWhiteSpace(drawName))
                {
                    var drawKey = $"{result.Tournament.ExternalCode}|{drawId}|{drawName}";
                    if (!drawsByKey.TryGetValue(drawKey, out var draw))
                    {
                        draw = new Draw
                        {
                            Name = drawName,
                            ExternalDrawId = drawId,
                            Tournament = result.Tournament
                            // EventId will be set when saving to database by matching name prefix
                        };

                        drawsByKey[drawKey] = draw;
                        result.Draws.Add(draw);
                        result.Tournament.Draws.Add(draw);
                    }

                    match.Draw = draw;
                    draw.Matches.Add(match);

                    if (!string.IsNullOrWhiteSpace(roundName))
                    {
                        var roundKey = $"{drawKey}|{roundName}";
                        if (!roundsByKey.TryGetValue(roundKey, out var round))
                        {
                            round = new Round
                            {
                                Name = roundName,
                                Draw = draw
                            };

                            roundsByKey[roundKey] = round;
                            result.Rounds.Add(round);
                            draw.Rounds.Add(round);
                        }

                        match.Round = round;
                        round.Matches.Add(match);
                    }
                }

                var courtName = ExtractCourt(matchNode);
                if (string.IsNullOrWhiteSpace(courtName))
                {
                    throw new InvalidOperationException("Missing court name for match.");
                }

                if (!string.IsNullOrWhiteSpace(courtName))
                {
                    var courtKey = $"{result.Tournament.ExternalCode}|{courtName}";
                    if (!courtsByKey.TryGetValue(courtKey, out var court))
                    {
                        court = new Court
                        {
                            Name = courtName
                        };

                        courtsByKey[courtKey] = court;
                        result.Courts.Add(court);
                    }

                    match.Court = court;
                    court.Matches.Add(match);
                }

                var (player1, player2, winnerPlayer, player1Walkover, player2Walkover) = ExtractPlayers(matchNode, nationalitiesByCode, playersByKey, result);

                result.Tournament.Matches.Add(match);
                result.TournamentDay.Matches.Add(match);

                match.Player1 = player1;
                match.Player2 = player2;
                match.WinnerPlayer = winnerPlayer;
                match.Player1Walkover = player1Walkover;
                match.Player2Walkover = player2Walkover;

                foreach (var game in ExtractGames(matchNode))
                {
                    game.Match = match;
                    match.Games.Add(game);
                    result.Games.Add(game);
                }

                var h2hHref = matchNode.SelectSingleNode(".//a[contains(@class,'match__btn-h2h')]")
                    ?.GetAttributeValue("href", null);

                if (!string.IsNullOrWhiteSpace(h2hHref))
                {
                    match.HeadToHeadUrl = h2hHref;
                    var h2hParams = ParseQueryParams(h2hHref);

                    if (h2hParams.TryGetValue("OrganizationCode", out var orgCode) && result.Tournament != null)
                    {
                        result.Tournament.OrganizationCode = orgCode;
                    }
                    else
                    {
                        Console.WriteLine();
                    }

                    if (h2hParams.TryGetValue("T1P1MemberID", out var member1))
                    {
                        if (player1 != null && string.IsNullOrWhiteSpace(player1.EsfMemberId))
                        {
                            player1.EsfMemberId = member1;
                        }
                    }
                    else
                    {
                        Console.WriteLine();
                    }

                    if (h2hParams.TryGetValue("T2P1MemberID", out var member2))
                    {
                        if (player2 != null && string.IsNullOrWhiteSpace(player2.EsfMemberId))
                        {
                            player2.EsfMemberId = member2;
                        }
                    }                   
                    else
                    {
                        Console.WriteLine();
                    }
                }

                result.Matches.Add(match);
            }

            return result;
        }

        private static string NormalizeHtml(string html)
        {
            if (!html.Contains("line-wrap") || !html.Contains("line-content"))
            {
                return html;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var lines = doc.DocumentNode.SelectNodes("//td[@class='line-content']");
            if (lines == null)
            {
                return html;
            }

            var builder = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                var text = HtmlEntity.DeEntitize(line.InnerText);
                builder.AppendLine(text);
            }

            return builder.ToString();
        }

        private static string ExtractTournamentName(HtmlDocument doc)
        {
            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidOperationException("Document title is missing.");
            }

            var prefix = "Matches - ";
            var separator = " | ";
            if (title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var start = prefix.Length;
                var end = title.IndexOf(separator, start, StringComparison.Ordinal);
                if (end > start)
                {
                    return title.Substring(start, end - start).Trim();
                }
            }

            return title;
        }

        private static (string code, DateTime date, string? location) ExtractMatchesInit(HtmlDocument doc)
        {
            var scriptNodes = doc.DocumentNode.SelectNodes("//script");
            if (scriptNodes == null)
            {
                throw new Exception("No script nodes found in the document. Cannot get date");
            }

            foreach (var script in scriptNodes)
            {
                var text = script.InnerText;
                var index = text.IndexOf("visualreality.app.tournament.matches.init", StringComparison.OrdinalIgnoreCase);
                if (index < 0)
                {
                    continue;
                }

                var start = text.IndexOf("(", index, StringComparison.Ordinal);
                var end = text.IndexOf(");", start, StringComparison.Ordinal);
                if (start < 0 || end < 0)
                {
                    continue;
                }

                var json = text.Substring(start + 1, end - start - 1);
                try
                {
                    using var docJson = JsonDocument.Parse(json);
                    if (!docJson.RootElement.TryGetProperty("postParamObject", out var postParams))
                    {
                        throw new InvalidOperationException("postParamObject property is missing in the JSON.");
                    }

                    string code = postParams.TryGetProperty("code", out var codeProp) ? codeProp.GetString() : null;
                    if (string.IsNullOrWhiteSpace(code))
                    {
                        throw new InvalidOperationException("Tournament code is missing in the JSON.");
                    }
                    string? dateText = postParams.TryGetProperty("date", out var dateProp) ? dateProp.GetString() : null;
                    string? location = postParams.TryGetProperty("location", out var locationProp) ? locationProp.GetString() : null;

                    if (string.IsNullOrWhiteSpace(dateText))
                    {
                        throw new InvalidOperationException("Invalid date format in the JSON.");
                    }
                    if (DateTime.TryParseExact(dateText, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed))
                    {
                        return (code, parsed, location);
                    }
                }
                catch (JsonException)
                {
                    throw new InvalidOperationException("Failed to parse JSON in the script node.");
                }
            }

            throw new InvalidOperationException("Failed to parse JSON in the script node.");
        }

        private static string? ExtractStartTimeText(HtmlNode matchNode)
        {
            var timeHeader = matchNode.SelectSingleNode("ancestor::li[contains(@class,'match-group__item')][1]//h5[contains(@class,'match-group__header')]");
            var text = timeHeader?.InnerText?.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                var fallbackHeader = matchNode.SelectSingleNode("preceding::h5[contains(@class,'match-group__header')][1]");
                text = fallbackHeader?.InnerText?.Trim();
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var match = System.Text.RegularExpressions.Regex.Match(text, "\\b\\d{1,2}:\\d{2}\\b");
            return match.Success ? match.Value : text;
        }

        private static (string? drawName, int? drawId, string? roundName) ExtractDrawAndRound(HtmlNode matchNode)
        {
            var headerItems = matchNode.SelectNodes(".//div[contains(@class,'match__header')]//ul[contains(@class,'match__header-title')]/li");
            if (headerItems != null && headerItems.Count > 0)
            {
                var drawName = ExtractNavValue(headerItems[0]);
                var drawId = ExtractDrawId(headerItems[0].SelectSingleNode(".//a")?.GetAttributeValue("href", null));

                string? roundName = null;
                if (headerItems.Count > 1)
                {
                    roundName = ExtractNavValue(headerItems[1]);
                }

                return (drawName, drawId, roundName);
            }

            var header = matchNode.SelectSingleNode(".//div[contains(@class,'match__header')]");
            if (header == null)
            {
                return (null, null, null);
            }

            var drawLink = header.SelectSingleNode(".//a[contains(@href,'draw=')]");
            var fallbackDrawName = drawLink?.SelectSingleNode(".//span[contains(@class,'nav-link__value')]")?.InnerText?.Trim();
            var fallbackDrawId = ExtractDrawId(drawLink?.GetAttributeValue("href", null));

            return (fallbackDrawName, fallbackDrawId, null);
        }

        private static string? ExtractNavValue(HtmlNode node)
        {
            return node.SelectSingleNode(".//span[contains(@class,'nav-link__value')]")?.InnerText?.Trim();
        }

        private static int? ExtractDrawId(string? href)
        {
            if (string.IsNullOrWhiteSpace(href))
            {
                return null;
            }

            var parameters = ParseQueryParams(href);
            if (parameters.TryGetValue("draw", out var drawText) && int.TryParse(drawText, out var drawId))
            {
                return drawId;
            }

            return null;
        }

        private static string? ExtractCourt(HtmlNode matchNode)
        {
            var footerValue = matchNode.SelectSingleNode(".//div[contains(@class,'match__footer')]//span[contains(@class,'nav-link__value')]");
            if (footerValue != null && footerValue.InnerText?.Trim() != "")
            {
                return footerValue.InnerText?.Trim();
            }

            var headerAside = matchNode.SelectSingleNode(".//div[contains(@class,'match__header-aside')]//span[contains(@class,'match__header-aside-block')]");
            var title = headerAside?.GetAttributeValue("title", null);
            return title?.Trim();
        }

        private static (Player? player1, Player? player2, Player? winner, bool player1Walkover, bool player2Walkover) ExtractPlayers(
            HtmlNode matchNode,
            Dictionary<string, Country> nationalitiesByCode,
            Dictionary<string, Player> playersByKey,
            DayMatchesParseResult result)
        {
            //var rows = matchNode.SelectNodes(".//div[contains(@class,'match__row')]");
            //var rows = matchNode.SelectNodes(".//div[@class,'match__body']/div[@class,'match__row-wrapper']/div");
            var rows = matchNode.SelectNodes(".//div[contains(@class,'match__body')]/div[contains(@class,'match__row-wrapper')]/div");

            if (rows == null || rows.Count != 2)
            {
                return (null, null, null, false, false);
            }

            Player? player1 = null;
            Player? player2 = null;
            Player? winner = null;
            bool player1Walkover = false;
            bool player2Walkover = false;

            for (var i = 0; i < rows.Count && i < 2; i++)
            {
                var row = rows[i];
                var side = i + 1;
                var rowClass = row.GetAttributeValue("class", string.Empty);
                var isWinner = rowClass.Contains("has-won", StringComparison.OrdinalIgnoreCase)
                               || row.SelectSingleNode(".//span[contains(@class,'match__status') and normalize-space(text())='W']") != null;

                // Check for walkover indicators (ESF varies: spans, divs, or inline text like "W/O")
                var isWalkover = IsWalkover(row);

                var playerLink = row.SelectSingleNode(".//a[@data-player-id]")
                                ?? row.SelectSingleNode(".//a[contains(@href,'player.aspx')]");

                var playerName = playerLink?.SelectSingleNode(".//span[contains(@class,'nav-link__value')]")?.InnerText?.Trim()
                                 ?? playerLink?.InnerText?.Trim();
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    // Skip this row - could be Bye/Walkover, will set player1/player2 to null for this side
                    if (side == 1)
                    {
                        player1 = null;
                        player1Walkover = isWalkover;
                    }
                    else if (side == 2)
                    {
                        player2 = null;
                        player2Walkover = isWalkover;
                    }
                    continue;
                }
                playerName = System.Text.RegularExpressions.Regex.Replace(playerName, "\\s*\\[[^\\]]*\\]\\s*$", string.Empty).Trim();
                playerName = HtmlEntity.DeEntitize(playerName);

                var playerIdText = playerLink?.GetAttributeValue("data-player-id", null);
                int? playerId = null;
                if (!string.IsNullOrWhiteSpace(playerIdText) && int.TryParse(playerIdText, out var parsedId))
                {
                    playerId = parsedId;
                }
                else
                {
                    var href = playerLink?.GetAttributeValue("href", null);
                    if (!string.IsNullOrWhiteSpace(href))
                    {
                        var query = ParseQueryParams(href);
                        if (query.TryGetValue("player", out var playerParam) && int.TryParse(playerParam, out var parsedFromHref))
                        {
                            playerId = parsedFromHref;
                        }
                    }
                }

                var nationalityCode = playerLink?.GetAttributeValue("data-nationality-id", null);
                if (string.IsNullOrWhiteSpace(nationalityCode))
                {
                    nationalityCode = row.SelectSingleNode(".//img[contains(@class,'icon-lang')]")?.GetAttributeValue("alt", null);
                }
                nationalityCode = nationalityCode?.Trim();

                if ((playerId.HasValue || !string.IsNullOrWhiteSpace(playerName)) && string.IsNullOrWhiteSpace(nationalityCode))
                {
                    throw new InvalidOperationException($"Missing nationality code for player '{playerName ?? "unknown"}'.");
                }

                Country? nationality = null;
                if (!string.IsNullOrWhiteSpace(nationalityCode))
                {
                    if (!nationalitiesByCode.TryGetValue(nationalityCode, out nationality))
                    {
                        nationality = new Country { Code = nationalityCode };
                        nationalitiesByCode[nationalityCode] = nationality;
                        result.Countries.Add(nationality);
                    }
                }

                Player? player = null;
                if (!string.IsNullOrWhiteSpace(playerName) || playerId.HasValue)
                {
                    var playerKey = $"{playerId ?? 0}|{playerName}|{nationalityCode}";
                    if (!playersByKey.TryGetValue(playerKey, out player))
                    {
                        player = new Player
                        {
                            Name = playerName ?? string.Empty,
                            Country = nationality,
                            EntitySourceId = EntitySource.Esf
                        };

                        playersByKey[playerKey] = player;
                        result.Players.Add(player);

                        if (nationality != null)
                        {
                            nationality.Players.Add(player);
                        }
                    }
                    
                    // Track external player ID for this tournament
                    if (playerId.HasValue && !result.TournamentPlayerIds.ContainsKey(player))
                    {
                        result.TournamentPlayerIds[player] = playerId.Value;
                    }
                }

                if (side == 1)
                {
                    player1 = player;
                    player1Walkover = isWalkover;
                }
                else if (side == 2)
                {
                    player2 = player;
                    player2Walkover = isWalkover;
                }

                if (isWinner && player != null)
                {
                    winner = player;
                }
            }

            return (player1, player2, winner, player1Walkover, player2Walkover);
        }

        private static IEnumerable<MatchGame> ExtractGames(HtmlNode matchNode)
        {
            var results = new List<MatchGame>();
            var pointSets = matchNode.SelectNodes(".//div[contains(@class,'match__result')]//ul[contains(@class,'points')]");
            if (pointSets == null)
            {
                return results;
            }

            var gameNumber = 1;
            foreach (var set in pointSets)
            {
                var cells = set.SelectNodes(".//li");
                if (cells == null || cells.Count < 2)
                {
                    continue;
                }

                int? side1 = TryParsePoints(cells[0].InnerText);
                int? side2 = TryParsePoints(cells[1].InnerText);

                int? winnerSide = null;
                if (cells[0].GetAttributeValue("class", string.Empty).Contains("points__cell--won", StringComparison.OrdinalIgnoreCase))
                {
                    winnerSide = 1;
                }
                else if (cells[1].GetAttributeValue("class", string.Empty).Contains("points__cell--won", StringComparison.OrdinalIgnoreCase))
                {
                    winnerSide = 2;
                }
                else if (side1.HasValue && side2.HasValue)
                {
                    winnerSide = side1 > side2 ? 1 : side2 > side1 ? 2 : null;
                }

                results.Add(new MatchGame
                {
                    GameNumber = gameNumber,
                    Side1Points = side1,
                    Side2Points = side2,
                    WinnerSide = winnerSide
                });

                gameNumber++;
            }

            return results;
        }

        private static int? TryParsePoints(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }

        private static bool IsWalkover(HtmlNode row)
        {
            // Common markup: dedicated message span
            var tag = row.SelectSingleNode(".//span[contains(@class,'match__message') and contains(translate(normalize-space(text()), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'walkover')]")
                      ?? row.SelectSingleNode(".//span[contains(@class,'tag--warning') and contains(translate(normalize-space(text()), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'walkover')]")
                      ?? row.SelectSingleNode(".//span[contains(@class,'match__status') and contains(translate(normalize-space(text()), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'w/o')]")
                      ?? row.SelectSingleNode(".//span[contains(@class,'tag--success') and contains(translate(normalize-space(text()), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'w/o')]");
            if (tag != null)
            {
                return true;
            }

            // Alternate markup: text node with W/O or Walkover near the player block
            var text = row.InnerText ?? string.Empty;
            if (text.IndexOf("walkover", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (text.IndexOf("w/o", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }

        private static Dictionary<string, string> ParseQueryParams(string href)
        {
            var decodedHref = System.Net.WebUtility.HtmlDecode(href);
            var queryIndex = decodedHref.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex < 0)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var query = decodedHref.Substring(queryIndex + 1);
            var parts = query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2)
                {
                    result[kv[0]] = Uri.UnescapeDataString(kv[1]);
                }
            }

            return result;
        }
    }

    public sealed class DayMatchesParseResult(Tournament tournament, TournamentDay tournamentDay)
    {
        public Tournament Tournament { get; set; } = tournament;
        public TournamentDay TournamentDay { get; set; } = tournamentDay;

        public List<Draw> Draws { get; } = new();
        public List<Round> Rounds { get; } = new();
        public List<Court> Courts { get; } = new();
        public List<Country> Countries { get; } = new();
        public List<Player> Players { get; } = new();
        public List<Match> Matches { get; } = new();
        public List<MatchGame> Games { get; } = new();
        
        // Maps Player to their TournamentPlayerId (ExternalPlayerId from ESF)
        public Dictionary<Player, int> TournamentPlayerIds { get; } = new();
    }
}

