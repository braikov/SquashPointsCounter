namespace Squash.Web.Areas.Public.Models
{
    public class TournamentListViewModel : PageViewModel
    {
        public string Culture { get; set; } = "en";
        public IReadOnlyCollection<TournamentListItemViewModel> Items { get; set; } = Array.Empty<TournamentListItemViewModel>();
    }
}
