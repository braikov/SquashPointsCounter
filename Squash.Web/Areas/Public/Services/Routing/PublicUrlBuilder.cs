using Microsoft.AspNetCore.Http;

namespace Squash.Web.Areas.Public.Services.Routing
{
    public class PublicUrlBuilder : IPublicUrlBuilder
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string? _configuredBaseUrl;

        public PublicUrlBuilder(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuredBaseUrl = configuration["PublicSite:BaseUrl"]?.TrimEnd('/');
        }

        public string BuildUrl(string cultureSegment, string relativePath)
        {
            var baseUrl = GetBaseUrl();
            var normalizedPath = NormalizeRelativePath(relativePath);
            return $"{baseUrl}/{cultureSegment}{normalizedPath}";
        }

        public string BuildTournamentUrl(string cultureSegment, int tournamentId, string? slug)
        {
            var slugPart = string.IsNullOrWhiteSpace(slug) ? tournamentId.ToString() : slug;
            return BuildUrl(cultureSegment, $"/tournament/{tournamentId}/{slugPart}");
        }

        private string GetBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(_configuredBaseUrl))
            {
                return _configuredBaseUrl;
            }

            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
            {
                return "https://localhost";
            }

            return $"{request.Scheme}://{request.Host}";
        }

        private static string NormalizeRelativePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return string.Empty;
            }

            return relativePath.StartsWith("/", StringComparison.Ordinal) ? relativePath : $"/{relativePath}";
        }
    }
}
