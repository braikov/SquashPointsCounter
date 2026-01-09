namespace Squash.DataAccess.Entities
{
    public class Draw : Squash.DataAccess.Entities.EntityBase
    {
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null!;

        public int? ExternalDrawId { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<Round> Rounds { get; set; } = new List<Round>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}