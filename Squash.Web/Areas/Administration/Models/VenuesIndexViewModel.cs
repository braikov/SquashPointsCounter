using System.Collections.Generic;
using System.Linq;

namespace Squash.Web.Areas.Administration.Models
{
    public class VenuesIndexViewModel
    {
        public IReadOnlyList<VenueListItemViewModel> Venues { get; set; } = new List<VenueListItemViewModel>();
        public int TotalCount { get; set; }
    }

    public class VenueListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? Zip { get; set; }
        public string? Region { get; set; }
        public string? CountryName { get; set; }
        public string? CountryCode { get; set; }
        public double? Longitude { get; set; }
        public double? Latitude { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public int CourtsCount { get; set; }
        public int TournamentsCount { get; set; }

        public string FormattedAddress
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(Street)) parts.Add(Street);

                var cityLine = string.Join(" ", new[] { Zip, City }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(cityLine)) parts.Add(cityLine);

                if (!string.IsNullOrWhiteSpace(Region)) parts.Add(Region);
                if (!string.IsNullOrWhiteSpace(CountryName)) parts.Add(CountryName);

                return parts.Count > 0 ? string.Join(", ", parts) : "-";
            }
        }
    }
}
