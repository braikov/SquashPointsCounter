using System.ComponentModel.DataAnnotations;

namespace Squash.DataAccess.Entities
{
    public class User : EntityBase
    {
        [Required]
        [MaxLength(450)]
        public string IdentityUserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public DateTime BirthDate { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        public int? CountryId { get; set; }
        public Country? Country { get; set; }

        [MaxLength(50)]
        public string? PreferredSport { get; set; }

        [MaxLength(10)]
        public string? PreferredLanguage { get; set; }
        public Language? Language { get; set; }

        [Required]
        [MaxLength(20)]
        public string Zip { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? BadgeId { get; set; }

        public bool Verified { get; set; }

        public DateTime? VerificationDate { get; set; }

        public bool EmailNotificationsEnabled { get; set; } = true;

        [MaxLength(128)]
        public string? StripeCustomerId { get; set; }

        public ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
        public int? PlayerId { get; set; }
        public Player? Player { get; set; }
    }
}

