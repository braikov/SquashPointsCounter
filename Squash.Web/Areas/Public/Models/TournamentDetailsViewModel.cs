namespace Squash.Web.Areas.Public.Models
{
    public class TournamentDetailsViewModel : PageViewModel
    {
        public string Culture { get; set; } = "en";
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Regulations { get; set; }
    }
}
