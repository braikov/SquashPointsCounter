namespace Squash.DataAccess.Entities
{
    public class Round : Squash.DataAccess.Entities.EntityBase
    {
        public int DrawId { get; set; }
        public Draw Draw { get; set; } = null!;

        public string Name { get; set; } = string.Empty;

        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
}