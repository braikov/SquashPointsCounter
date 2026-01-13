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

        [Required]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public DateTime BirthDate { get; set; }

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
    }
}
