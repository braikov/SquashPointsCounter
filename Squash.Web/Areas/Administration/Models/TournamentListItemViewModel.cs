namespace Squash.Web.Areas.Administration.Models
{
    public class TournamentListItemViewModel
    {
        public int Id { get; set; }
        public string ExternalCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OrganizationCode { get; set; }
        public int DaysCount { get; set; }
        public int DrawsCount { get; set; }
        public int CourtsCount { get; set; }
        public int MatchesCount { get; set; }
        public int? FirstDayId { get; set; }
    }
}
