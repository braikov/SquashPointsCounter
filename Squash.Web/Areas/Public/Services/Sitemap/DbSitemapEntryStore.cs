using Microsoft.EntityFrameworkCore;
using Squash.SqlServer;
using Squash.Web.Areas.Public.Models;

namespace Squash.Web.Areas.Public.Services.Sitemap
{
    public class DbSitemapEntryStore : ISitemapEntryStore
    {
        private readonly DataContext _dataContext;

        public DbSitemapEntryStore(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        public async Task<IReadOnlyCollection<SitemapEntryItem>> GetEnabledEntriesAsync(string cultureSegment, CancellationToken cancellationToken)
        {
            var normalizedCulture = NormalizeCulture(cultureSegment);

            var entries = await _dataContext.SitemapEntries
                .AsNoTracking()
                .Where(entry => entry.IsEnabled
                                && (entry.Culture == null
                                    || entry.Culture == string.Empty
                                    || entry.Culture == normalizedCulture
                                    || entry.Culture == cultureSegment))
                .Select(entry => new SitemapEntryItem
                {
                    Url = entry.Url,
                    ChangeFrequency = entry.ChangeFrequency,
                    Priority = entry.Priority,
                    LastModifiedUtc = entry.DateUpdated
                })
                .ToListAsync(cancellationToken);

            return entries;
        }

        private static string? NormalizeCulture(string cultureSegment)
        {
            if (string.IsNullOrWhiteSpace(cultureSegment))
            {
                return null;
            }

            return cultureSegment.Equals("bg", StringComparison.OrdinalIgnoreCase) ? "bg-BG"
                : cultureSegment.Equals("en", StringComparison.OrdinalIgnoreCase) ? "en-GB"
                : cultureSegment;
        }
    }
}
