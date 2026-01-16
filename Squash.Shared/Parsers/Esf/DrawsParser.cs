using System;
using System.Collections.Generic;
using System.Globalization;
using HtmlAgilityPack;
using Squash.DataAccess.Entities;

namespace Squash.Shared.Parsers.Esf
{
    public class DrawsParser
    {
        public DrawsParseResult Parse(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new ArgumentException("HTML content is empty.", nameof(html));
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var result = new DrawsParseResult();
            var table = doc.DocumentNode.SelectSingleNode("//table[contains(@class,'ruler')][.//thead//td[normalize-space()='Draw']]");
            var rows = table?.SelectNodes(".//tbody/tr");
            if (rows == null)
            {
                return result;
            }

            foreach (var row in rows)
            {
                var link = row.SelectSingleNode(".//td[contains(@class,'drawname')]//a");
                var name = NormalizeText(link?.InnerText);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var drawId = ExtractDrawId(link?.GetAttributeValue("href", null));
                var typeCell = row.SelectSingleNode("./td[3]");
                var type = NormalizeText(typeCell?.InnerText);

                result.Draws.Add(new Draw
                {
                    Name = name,
                    ExternalDrawId = drawId,
                    Type = type
                });
            }

            return result;
        }

        private static int? ExtractDrawId(string? href)
        {
            if (string.IsNullOrWhiteSpace(href))
            {
                return null;
            }

            var parameters = ParseQueryParams(href);
            if (parameters.TryGetValue("draw", out var drawText)
                && int.TryParse(drawText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var drawId))
            {
                return drawId;
            }

            return null;
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

    public sealed class DrawsParseResult
    {
        public List<Draw> Draws { get; } = new();
    }
}
