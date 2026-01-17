using Microsoft.AspNetCore.Mvc.Rendering;

namespace Squash.Web.Areas.Administration.Models
{
    public class TournamentVenuesViewModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public int? NationalityId { get; set; }

        public List<TournamentVenueItemViewModel> AssignedVenues { get; set; } = new();
        public List<SelectListItem> AvailableVenues { get; set; } = new();
        
        public int? SelectedVenueId { get; set; }
    }

    public class TournamentVenueItemViewModel
    {
        public int VenueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? City { get; set; }
        public List<TournamentCourtItemViewModel> Courts { get; set; } = new();
    }

    public class TournamentCourtItemViewModel
    {
        public int CourtId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsAssignedToTournament { get; set; }
    }
}
