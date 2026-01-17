using Squash.Web.Areas.Public.Models;

namespace Squash.Web.Areas.Public.Services.Sitemap
{
    public interface ISitemapBuilder
    {
        Task<IReadOnlyCollection<SitemapEntryItem>> BuildAsync(string cultureSegment, CancellationToken cancellationToken);
    }
}
