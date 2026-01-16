namespace Squash.Web.Areas.Administration.Models
{
    public class TournamentEventsViewModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public List<TournamentEventListItemViewModel> Events { get; set; } = new();
        public TournamentEventEditViewModel EventForm { get; set; } = new();
    }
}
