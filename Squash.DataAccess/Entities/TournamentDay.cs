namespace Squash.DataAccess.Entities
{
    public class TournamentDay : Squash.DataAccess.Entities.EntityBase
    {
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null!;

        public DateTime Date { get; set; }
        public string? LocationFilter { get; set; }

        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}