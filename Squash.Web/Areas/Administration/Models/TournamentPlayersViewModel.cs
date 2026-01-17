using System.Collections.Generic;

namespace Squash.Web.Areas.Administration.Models
{
    public class TournamentPlayersViewModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public bool IsPublished { get; set; }

        public List<TournamentPlayerGroupViewModel> PlayerGroups { get; set; } = new();

        public string? FilterEvent { get; set; }
        public string? FilterCountry { get; set; }
        public string? FilterDraw { get; set; }
        public string? FilterRound { get; set; }
        public string? FilterCourt { get; set; }

        public List<FilterOption> AvailableEvents { get; set; } = new();
        public List<FilterOption> AvailableCountries { get; set; } = new();
        public List<FilterOption> AvailableDraws { get; set; } = new();
        public List<FilterOption> AvailableRounds { get; set; } = new();
        public List<FilterOption> AvailableCourts { get; set; } = new();

        public int TotalPlayersCount { get; set; }
        public int FilteredPlayersCount { get; set; }
    }

    public class TournamentPlayerGroupViewModel
    {
        public string Letter { get; set; } = string.Empty;
        public List<TournamentPlayerItemViewModel> Players { get; set; } = new();
    }

    public class TournamentPlayerItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public string FlagUrl { get; set; } = string.Empty;
    }
}
