using Squash.Web.Areas.Public.Models;

namespace Squash.Web.Areas.Public.Services.Sitemap
{
    public class SitemapBuilder : ISitemapBuilder
    {
        private readonly IEnumerable<ISitemapEntryProvider> _providers;

        public SitemapBuilder(IEnumerable<ISitemapEntryProvider> providers)
        {
            _providers = providers;
        }

        public async Task<IReadOnlyCollection<SitemapEntryItem>> BuildAsync(string cultureSegment, CancellationToken cancellationToken)
        {
            var items = new Dictionary<string, SitemapEntryItem>(StringComparer.OrdinalIgnoreCase);

            foreach (var provider in _providers)
            {
                var entries = await provider.GetEntriesAsync(cultureSegment, cancellationToken);
                foreach (var entry in entries)
                {
                    if (string.IsNullOrWhiteSpace(entry.Url))
                    {
                        continue;
                    }

                    items[entry.Url] = entry;
                }
            }

            return items.Values.ToList();
        }
    }
}
