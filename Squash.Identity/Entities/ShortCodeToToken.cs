using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Squash.Identity.Entities
{
    public class ShortCodeToToken : EntityBase
    {
        [Required, MaxLength(256)]
        public string Email { get; set; } = null!;

        [Required, MaxLength(6)]
        public string Code { get; set; } = null!;

        [Required]
        public string Token { get; set; } = null!;
    }
}