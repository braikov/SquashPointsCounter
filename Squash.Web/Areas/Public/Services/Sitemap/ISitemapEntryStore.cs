using Squash.Web.Areas.Public.Models;

namespace Squash.Web.Areas.Public.Services.Sitemap
{
    public interface ISitemapEntryStore
    {
        Task<IReadOnlyCollection<SitemapEntryItem>> GetEnabledEntriesAsync(string cultureSegment, CancellationToken cancellationToken);
    }
}
