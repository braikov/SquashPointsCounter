namespace Squash.DataAccess.Entities
{
    public class Nationality : Squash.DataAccess.Entities.EntityBase
    {
        public string Code { get; set; } = string.Empty;
        public string? Name { get; set; }

        public ICollection<Player> Players { get; set; } = new List<Player>();
    }
}