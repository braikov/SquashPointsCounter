namespace Squash.DataAccess.Entities
{
    public class Tournament : Squash.DataAccess.Entities.EntityBase
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string ExternalCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OrganizationCode { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ClosingSigninDate { get; set; }
        public string? Regulations { get; set; }
        public EntitySource EntitySourceId { get; set; } = EntitySource.Native;
        public string? SourceUrls { get; set; }

        public ICollection<TournamentDay> Days { get; set; } = new List<TournamentDay>();
        public ICollection<Draw> Draws { get; set; } = new List<Draw>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
        public ICollection<TournamentVenue> TournamentVenues { get; set; } = new List<TournamentVenue>();
        public ICollection<TournamentCourt> TournamentCourts { get; set; } = new List<TournamentCourt>();
        public ICollection<PlayerTournament> PlayerTournaments { get; set; } = new List<PlayerTournament>();
    }
}
