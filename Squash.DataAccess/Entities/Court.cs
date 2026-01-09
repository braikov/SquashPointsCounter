namespace Squash.DataAccess.Entities
{
    public class Court : Squash.DataAccess.Entities.EntityBase
    {
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}