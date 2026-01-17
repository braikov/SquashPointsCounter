namespace Squash.Web.Areas.Public.Services.Routing
{
    public interface IPublicUrlBuilder
    {
        string BuildUrl(string cultureSegment, string relativePath);
        string BuildTournamentUrl(string cultureSegment, int tournamentId, string? slug);
    }
}
