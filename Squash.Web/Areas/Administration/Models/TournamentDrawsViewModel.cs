namespace Squash.Web.Areas.Administration.Models
{
    public class TournamentDrawsViewModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public string? EventName { get; set; }
        public List<TournamentDrawListItemViewModel> Draws { get; set; } = new();
        public List<TournamentEntryListItemViewModel> Entries { get; set; } = new();
    }
}
