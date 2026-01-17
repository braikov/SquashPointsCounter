using System.ComponentModel.DataAnnotations;

namespace Squash.DataAccess.Entities
{
    public class Language
    {
        [Key]
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
