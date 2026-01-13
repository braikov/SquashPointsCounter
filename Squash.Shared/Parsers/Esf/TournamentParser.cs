using System;
using System.Globalization;
using System.Linq;
using HtmlAgilityPack;
using Squash.DataAccess.Entities;

namespace Squash.Shared.Parsers.Esf
{
    public class TournamentParser
    {
        public TournamentParseResult Parse(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new ArgumentException("HTML content is empty.", nameof(html));
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var tournamentName = ExtractTournamentName(doc);
            var regulations = ExtractRegulations(doc);
            var closingDate = ExtractTimelineDate(doc, "Closing deadline");
            var startDate = ExtractTimelineDate(doc, "Start tournament");
            var endDate = ExtractTimelineDate(doc, "End of tournament");
            var contact = ExtractContact(doc);
            var venues = ExtractVenues(doc);

            var tournament = new Tournament
            {
                Name = tournamentName ?? string.Empty,
                ClosingSigninDate = closingDate,
                StartDate = startDate,
                EndDate = endDate,
                Regulations = regulations
            };

            return new TournamentParseResult
            {
                Tournament = tournament,
                ContactName = contact.name,
                ContactEmail = contact.email,
                ContactPhone = contact.phone,
                Venues = venues
            };
        }

        private static string? ExtractTournamentName(HtmlDocument doc)
        {
            var node = doc.DocumentNode.SelectSingleNode("//h2[contains(@class,'media__title')]//span[contains(@class,'nav-link__value')]");
            return NormalizeText(node?.InnerText);
        }

        private static string? ExtractRegulations(HtmlDocument doc)
        {
            var node = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'alert__body-inner')]");
            if (node == null)
            {
                return null;
            }

            var html = node.InnerHtml.Replace("\r", string.Empty).Trim();
            return string.IsNullOrWhiteSpace(html) ? null : html;
        }

        private static DateTime? ExtractTimelineDate(HtmlDocument doc, string label)
        {
            var nodes = doc.DocumentNode.SelectNodes("//li[contains(@class,'list__item')]");
            if (nodes == null)
            {
                return null;
            }

            foreach (var node in nodes)
            {
                var labelNode = node.SelectSingleNode(".//div[contains(@class,'list__value')]");
                var labelText = NormalizeText(labelNode?.InnerText);
                if (!string.Equals(labelText, label, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var timeNode = node.SelectSingleNode(".//time[@datetime]");
                var datetime = timeNode?.GetAttributeValue("datetime", null);
                if (string.IsNullOrWhiteSpace(datetime))
                {
                    continue;
                }

                if (DateTimeOffset.TryParse(datetime, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return parsed.DateTime;
                }
            }

            return null;
        }

        private static (string? name, string? email, string? phone) ExtractContact(HtmlDocument doc)
        {
            var modules = doc.DocumentNode.SelectNodes("//div[contains(@class,'module')]");
            if (modules == null)
            {
                return (null, null, null);
            }

            HtmlNode? selectedModule = null;
            foreach (var module in modules)
            {
                var title = NormalizeText(module.SelectSingleNode(".//span[contains(@class,'module__title-main')]")?.InnerText);
                if (string.Equals(title, "Contact", StringComparison.OrdinalIgnoreCase))
                {
                    selectedModule = module;
                    break;
                }
            }

            if (selectedModule == null)
            {
                selectedModule = modules.FirstOrDefault(m =>
                    m.SelectSingleNode(".//a[starts-with(@href,'mailto:') or starts-with(@href,'tel:')]") != null);
            }

            if (selectedModule == null)
            {
                return (null, null, null);
            }

            var emailNode = selectedModule.SelectSingleNode(".//a[starts-with(@href,'mailto:')]");
            var email = emailNode?.GetAttributeValue("href", string.Empty).Replace("mailto:", string.Empty);

            var phoneNode = selectedModule.SelectSingleNode(".//a[starts-with(@href,'tel:')]");
            var phone = phoneNode?.GetAttributeValue("href", string.Empty).Replace("tel:", string.Empty);
            if (string.IsNullOrWhiteSpace(phone))
            {
                phone = NormalizeText(phoneNode?.InnerText);
            }

            var name = NormalizeText(selectedModule.SelectSingleNode(".//span[contains(@class,'media__title')]")?.InnerText);
            if (string.IsNullOrWhiteSpace(name))
            {
                var listItems = selectedModule.SelectNodes(".//ul[contains(@class,'list--naked')]//li");
                if (listItems != null)
                {
                    foreach (var item in listItems)
                    {
                        if (item.SelectSingleNode(".//a[starts-with(@href,'mailto:') or starts-with(@href,'tel:')]") != null)
                        {
                            continue;
                        }

                        var text = NormalizeText(item.InnerText);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            name = text;
                            break;
                        }
                    }
                }
            }

            return (name, NormalizeText(email), NormalizeText(phone));
        }

        private static List<Venue> ExtractVenues(HtmlDocument doc)
        {
            var venues = new List<Venue>();
            var modules = doc.DocumentNode
                .SelectNodes("//div[contains(@class,'module')]")
                ?.Where(m => m.SelectSingleNode(".//*[contains(@class,'p-adr') or contains(@class,'p-street-address') or contains(@class,'p-country-name')]") != null)
                .ToList();

            if (modules == null || modules.Count == 0)
            {
                return venues;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var module in modules)
            {
                var mediaNodes = module.SelectNodes(".//div[contains(@class,'media')]");
                IEnumerable<HtmlNode> mediaItems = mediaNodes == null ? new[] { module } : mediaNodes;
                foreach (var media in mediaItems)
                {
                    var (name, address) = ExtractVenueInfo(media);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var key = $"{name}|{address}".Trim();
                    if (!seen.Add(key))
                    {
                        continue;
                    }

                    var (lat, lon) = ExtractCoordinates(module);
                    venues.Add(new Venue
                    {
                        Name = name,
                        Address = address ?? string.Empty,
                        Latitude = lat,
                        Longitude = lon
                    });
                }
            }

            return venues;
        }

        private static (string? name, string? address) ExtractVenueInfo(HtmlNode module)
        {
            var nameNode = module.SelectSingleNode(".//h5[contains(@class,'media__title')]//span[contains(@class,'nav-link__value')]");
            var name = NormalizeText(nameNode?.InnerText);

            var lines = new List<string>();
            var addressNode = module.SelectSingleNode(".//*[contains(@class,'p-adr')]");
            if (addressNode != null)
            {
                lines.AddRange(SplitLines(addressNode.InnerText));
            }

            var streetNode = module.SelectSingleNode(".//div[contains(@class,'p-street-address')]");
            if (streetNode != null)
            {
                lines.AddRange(SplitLines(streetNode.InnerText));
            }

            var postal = NormalizeText(module.SelectSingleNode(".//span[contains(@class,'p-postal-code')]")?.InnerText);
            var locality = NormalizeText(module.SelectSingleNode(".//span[contains(@class,'p-locality')]")?.InnerText);
            if (!string.IsNullOrWhiteSpace(postal) || !string.IsNullOrWhiteSpace(locality))
            {
                var cityLine = $"{postal} {locality}".Trim();
                if (!string.IsNullOrWhiteSpace(cityLine))
                {
                    lines.Add(cityLine);
                }
            }

            var country = NormalizeText(module.SelectSingleNode(".//div[contains(@class,'p-country-name')]")?.InnerText);
            if (!string.IsNullOrWhiteSpace(country))
            {
                lines.Add(country);
            }

            if (lines.Count == 0)
            {
                return (name, null);
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name = lines.FirstOrDefault();
                lines = lines.Skip(1).ToList();
            }

            if (!string.IsNullOrWhiteSpace(name) && lines.Count > 0
                && string.Equals(lines[0], name, StringComparison.OrdinalIgnoreCase))
            {
                lines.RemoveAt(0);
            }

            var address = lines.Count > 0 ? string.Join(Environment.NewLine, lines) : null;
            return (name, address);
        }

        private static (double? lat, double? lon) ExtractCoordinates(HtmlNode module)
        {
            var routeLink = module.SelectSingleNode(".//a[contains(@href,'maps/dir') and contains(@href,'destination=')]");
            var href = routeLink?.GetAttributeValue("href", string.Empty);
            if (string.IsNullOrWhiteSpace(href))
            {
                return (null, null);
            }

            var marker = "destination=";
            var index = href.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return (null, null);
            }

            var destination = href.Substring(index + marker.Length);
            var amp = destination.IndexOf("&", StringComparison.OrdinalIgnoreCase);
            if (amp >= 0)
            {
                destination = destination.Substring(0, amp);
            }

            destination = Uri.UnescapeDataString(destination);
            var parts = destination.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length >= 2
                && double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var lat)
                && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
            {
                return (lat, lon);
            }

            return (null, null);
        }

        private static IEnumerable<string> SplitLines(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            var normalized = HtmlEntity.DeEntitize(text).Replace("\r", string.Empty);
            foreach (var line in normalized.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    yield return trimmed;
                }
            }
        }

        private static string? NormalizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var normalized = HtmlEntity.DeEntitize(text);
            normalized = normalized.Replace("\r", string.Empty).Trim();
            return normalized;
        }
    }

    public class TournamentParseResult
    {
        public Tournament Tournament { get; set; } = new Tournament();
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public List<Venue> Venues { get; set; } = new List<Venue>();
    }
}
