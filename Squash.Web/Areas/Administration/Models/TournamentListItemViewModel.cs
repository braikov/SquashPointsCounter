namespace Squash.Web.Areas.Administration.Models
{
    public class TournamentListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int DaysCount { get; set; }
        public int DrawsCount { get; set; }
        public int CourtsCount { get; set; }
        public int MatchesCount { get; set; }
        public int PlayersCount { get; set; }
        public int? FirstDayId { get; set; }
    }
}
