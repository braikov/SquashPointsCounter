using System.ComponentModel.DataAnnotations;

namespace Squash.Identity.Entities
{
    public class AccountEvent : EntityBase
    {
        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(512)]
        public string? UserAgent { get; set; }

        [MaxLength(2048)]
        public string? MetadataJson { get; set; }
    }
}
