namespace Squash.Web.Areas.Administration.Models
{
    public class TournamentsIndexViewModel
    {
        public IReadOnlyList<TournamentListItemViewModel> Tournaments { get; set; } = [];
        public string? FilterStatus { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}
