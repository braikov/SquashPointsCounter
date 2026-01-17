using Microsoft.EntityFrameworkCore;
using Squash.SqlServer;
using Squash.Web.Areas.Public.Models;
using Squash.Web.Areas.Public.Services.Routing;

namespace Squash.Web.Areas.Public.Services.Sitemap
{
    public class TournamentSitemapEntryProvider : ISitemapEntryProvider
    {
        private readonly DataContext _dataContext;
        private readonly IPublicUrlBuilder _urlBuilder;

        public TournamentSitemapEntryProvider(DataContext dataContext, IPublicUrlBuilder urlBuilder)
        {
            _dataContext = dataContext;
            _urlBuilder = urlBuilder;
        }

        public async Task<IReadOnlyCollection<SitemapEntryItem>> GetEntriesAsync(string cultureSegment, CancellationToken cancellationToken)
        {
            var tournaments = await _dataContext.Tournaments
                .AsNoTracking()
                .Where(tournament => tournament.IsPublished)
                .Select(tournament => new
                {
                    tournament.Id,
                    tournament.Slug,
                    tournament.DateUpdated
                })
                .ToListAsync(cancellationToken);

            return tournaments.Select(tournament => new SitemapEntryItem
            {
                Url = _urlBuilder.BuildTournamentUrl(cultureSegment, tournament.Id, tournament.Slug),
                LastModifiedUtc = tournament.DateUpdated
            }).ToList();
        }
    }
}
