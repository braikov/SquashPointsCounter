using System.ComponentModel.DataAnnotations;

namespace Squash.Identity.Entities
{
    public class LoginShortCode : EntityBase
    {
        [Required, MaxLength(256)]
        public required string Email { get; set; }

        [Required, MaxLength(6)]
        public required string Code { get; set; }

        [Required]
        public required DateTime Expiration { get; set; }
    }
}