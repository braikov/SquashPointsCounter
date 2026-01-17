using System.ComponentModel.DataAnnotations;

namespace Squash.DataAccess.Entities
{
    public class Country : Squash.DataAccess.Entities.EntityBase
    {
        [MaxLength(10)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Nationality { get; set; }

        [MaxLength(100)]
        public string? CountryName { get; set; }

        public ICollection<Player> Players { get; set; } = new List<Player>();
        public ICollection<Venue> Venues { get; set; } = new List<Venue>();
    }
}
