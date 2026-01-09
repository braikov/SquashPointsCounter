namespace Squash.DataAccess.Entities
{
    public class MatchGame : Squash.DataAccess.Entities.EntityBase
    {
        public int MatchId { get; set; }
        public Match Match { get; set; } = null!;

        public int GameNumber { get; set; }
        public int? Side1Points { get; set; }
        public int? Side2Points { get; set; }
        public int? WinnerSide { get; set; }
    }
}