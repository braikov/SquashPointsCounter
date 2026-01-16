using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Squash.DataAccess.Entities;
using MatchType = Squash.DataAccess.Entities.MatchType;

namespace Squash.Shared.Parsers.Esf
{
    public class EventsParser
    {
        public EventsParseResult Parse(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new ArgumentException("HTML content is empty.", nameof(html));
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var result = new EventsParseResult();
            var rows = doc.DocumentNode.SelectNodes("//table[contains(@class,'admintournamentevents')]//tbody/tr");
            if (rows == null)
            {
                return result;
            }

            foreach (var row in rows)
            {
                var link = row.SelectSingleNode(".//td[contains(@class,'eventname')]//a");
                var name = NormalizeText(link?.InnerText);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var externalId = ExtractEventId(link?.GetAttributeValue("href", null));
                var (matchType, age, direction) = ParseEventName(name);

                result.Events.Add(new Event
                {
                    Name = name,
                    ExternalEventId = externalId,
                    MatchType = matchType,
                    Age = age,
                    Direction = direction
                });
            }

            return result;
        }

        private static int? ExtractEventId(string? href)
        {
            if (string.IsNullOrWhiteSpace(href))
            {
                return null;
            }

            var parameters = ParseQueryParams(href);
            if (parameters.TryGetValue("event", out var eventText)
                && int.TryParse(eventText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var eventId))
            {
                return eventId;
            }

            return null;
        }

        private static (MatchType matchType, int age, Direction direction) ParseEventName(string name)
        {
            var trimmed = name.Trim();
            var prefix = Regex.Match(trimmed, "^[A-Za-z]+").Value.ToUpperInvariant();
            var ageMatch = Regex.Match(trimmed, "\\d+");

            var direction = Direction.Under;
            if (trimmed.Contains("Above", StringComparison.OrdinalIgnoreCase)
                || trimmed.Contains("Over", StringComparison.OrdinalIgnoreCase)
                || prefix.Contains('O'))
            {
                direction = Direction.Above;
            }
            else if (prefix.Contains('U'))
            {
                direction = Direction.Under;
            }

            var matchType = MatchType.MenSingles;
            switch (prefix)
            {
                case "BU":
                case "MS":
                case "M":
                    matchType = MatchType.MenSingles;
                    break;
                case "GU":
                case "WS":
                case "W":
                    matchType = MatchType.WomenSingles;
                    break;
                case "BD":
                case "MD":
                    matchType = MatchType.MenDoubles;
                    break;
                case "GD":
                case "WD":
                    matchType = MatchType.WomenDoubles;
                    break;
                case "XD":
                case "MX":
                case "MIX":
                    matchType = MatchType.MixedDoubles;
                    break;
            }

            var age = 0;
            if (ageMatch.Success)
            {
                int.TryParse(ageMatch.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out age);
            }

            return (matchType, age, direction);
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

        private static string? NormalizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var normalized = HtmlEntity.DeEntitize(text);
            return normalized.Replace("\r", string.Empty).Trim();
        }
    }

    public sealed class EventsParseResult
    {
        public List<Event> Events { get; } = new();
    }
}
