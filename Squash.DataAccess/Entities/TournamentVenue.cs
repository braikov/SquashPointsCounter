namespace Squash.DataAccess.Entities
{
    public class TournamentVenue : EntityBase
    {
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null!;

        public int VenueId { get; set; }
        public Venue Venue { get; set; } = null!;
    }
}
