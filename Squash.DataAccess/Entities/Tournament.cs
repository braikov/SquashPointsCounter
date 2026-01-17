using Microsoft.EntityFrameworkCore;

namespace Squash.DataAccess.Entities
{
    [Index(nameof(Slug))]
    [Index(nameof(IsPublished))]
    public class Tournament : Squash.DataAccess.Entities.EntityBase
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int CountryId { get; set; }
        public Country Country { get; set; } = null!;

        public string ExternalCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OrganizationCode { get; set; }
        [System.ComponentModel.DataAnnotations.MaxLength(200)]
        public string? Slug { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? EntryOpensDate { get; set; }
        public DateTime? ClosingSigninDate { get; set; }
        public DateTime? WithdrawalDeadlineDate { get; set; }
        public string? Regulations { get; set; }
        public EntitySource EntitySourceId { get; set; } = EntitySource.Native;
        public string? SourceUrls { get; set; }

        public ICollection<TournamentDay> Days { get; set; } = new List<TournamentDay>();
        public ICollection<Event> Events { get; set; } = new List<Event>();
        public ICollection<Draw> Draws { get; set; } = new List<Draw>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
        public ICollection<TournamentVenue> TournamentVenues { get; set; } = new List<TournamentVenue>();
        public ICollection<TournamentCourt> TournamentCourts { get; set; } = new List<TournamentCourt>();
        public ICollection<TournamentPlayer> TournamentPlayers { get; set; } = new List<TournamentPlayer>();
    }
}

