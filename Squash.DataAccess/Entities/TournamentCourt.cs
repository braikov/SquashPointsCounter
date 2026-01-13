namespace Squash.DataAccess.Entities
{
    public class TournamentCourt : EntityBase
    {
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null!;

        public int CourtId { get; set; }
        public Court Court { get; set; } = null!;
    }
}
