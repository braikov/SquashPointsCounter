using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Squash.DataAccess.Entities
{
    public class TimeZone : EntityBase
    {
        [Required]
        [MaxLength(100)]
        public string StandardName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Offset from UTC in hours (e.g., 2.0 or -5.5).
        /// </summary>
        public double Offset { get; set; }
    }
}
