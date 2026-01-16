namespace Squash.DataAccess.Entities
{
    public class TournamentPlayer : EntityBase
    {
        public int PlayerId { get; set; }
        public Player Player { get; set; } = null!;

        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null!;

        public int? TournamentPlayerId { get; set; }
    }
}
