using Squash.Web.Areas.Public.Models;

namespace Squash.Web.Areas.Public.Services.Sitemap
{
    public interface ISitemapEntryProvider
    {
        Task<IReadOnlyCollection<SitemapEntryItem>> GetEntriesAsync(string cultureSegment, CancellationToken cancellationToken);
    }
}
