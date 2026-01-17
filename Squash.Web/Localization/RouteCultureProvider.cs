using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Routing;

namespace Squash.Web.Localization
{
    public class RouteCultureProvider : RequestCultureProvider
    {
        private static readonly IReadOnlyDictionary<string, string> CultureMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["bg"] = "bg-BG",
                ["en"] = "en-GB"
            };

        public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var cultureSegment = httpContext.GetRouteValue("culture") as string;
            if (string.IsNullOrWhiteSpace(cultureSegment))
            {
                return Task.FromResult<ProviderCultureResult?>(null);
            }

            return CultureMap.TryGetValue(cultureSegment, out var mappedCulture)
                ? Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(mappedCulture, mappedCulture))
                : Task.FromResult<ProviderCultureResult?>(null);
        }
    }
}
