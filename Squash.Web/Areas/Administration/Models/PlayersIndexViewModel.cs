using System.Collections.Generic;

namespace Squash.Web.Areas.Administration.Models
{
    public class PlayersIndexViewModel
    {
        public IReadOnlyList<PlayerListItemViewModel> Players { get; set; } = new List<PlayerListItemViewModel>();

        public string? FilterCountry { get; set; }
        public List<FilterOption> AvailableCountries { get; set; } = new();

        public int TotalCount { get; set; }
        public int FilteredCount { get; set; }
    }

    public class PlayerListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string CountryName { get; set; } = string.Empty;
        public string FlagUrl { get; set; } = string.Empty;
        public string EsfMemberId { get; set; } = string.Empty;
        public int? ExternalPlayerId { get; set; }
    }
}
