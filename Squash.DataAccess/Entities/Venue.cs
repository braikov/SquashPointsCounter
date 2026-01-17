using System.ComponentModel.DataAnnotations;

namespace Squash.DataAccess.Entities
{
    public class Venue : EntityBase
    {
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Street { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(20)]
        public string? Zip { get; set; }

        [MaxLength(100)]
        public string? Region { get; set; }

        public int? CountryId { get; set; }
        public Country? Country { get; set; }

        public double? Longitude { get; set; }
        public double? Latitude { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Website { get; set; }

        public ICollection<Court> Courts { get; set; } = new List<Court>();
        public ICollection<TournamentVenue> TournamentVenues { get; set; } = new List<TournamentVenue>();
    }
}

