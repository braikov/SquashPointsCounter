using Squash.Web.Areas.Public.Models;
using Squash.Web.Areas.Public.Services.Routing;

namespace Squash.Web.Areas.Public.Services.Sitemap
{
    public class DbSitemapEntryProvider : ISitemapEntryProvider
    {
        private readonly ISitemapEntryStore _store;
        private readonly IPublicUrlBuilder _urlBuilder;

        public DbSitemapEntryProvider(ISitemapEntryStore store, IPublicUrlBuilder urlBuilder)
        {
            _store = store;
            _urlBuilder = urlBuilder;
        }

        public async Task<IReadOnlyCollection<SitemapEntryItem>> GetEntriesAsync(string cultureSegment, CancellationToken cancellationToken)
        {
            var entries = await _store.GetEnabledEntriesAsync(cultureSegment, cancellationToken);
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Url))
                {
                    continue;
                }

                if (entry.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || entry.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var normalizedPath = entry.Url.StartsWith("/bg", StringComparison.OrdinalIgnoreCase)
                    || entry.Url.StartsWith("/en", StringComparison.OrdinalIgnoreCase)
                    ? entry.Url.Substring(3)
                    : entry.Url;

                entry.Url = _urlBuilder.BuildUrl(cultureSegment, normalizedPath);
            }

            return entries;
        }
    }
}
