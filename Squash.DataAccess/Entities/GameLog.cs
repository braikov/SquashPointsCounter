namespace Squash.DataAccess.Entities
{
    public class GameLog : EntityBase
    {
        public int MatchGameId { get; set; }
        public MatchGame MatchGame { get; set; } = null!;

        public MatchGameEvent Event { get; set; }
    }
}
