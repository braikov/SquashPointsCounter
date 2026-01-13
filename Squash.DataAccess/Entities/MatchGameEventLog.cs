namespace Squash.DataAccess.Entities
{
    public class MatchGameEventLog
    {
        public long Id { get; set; }

        public int MatchGameId { get; set; }
        public MatchGame MatchGame { get; set; } = null!;

        public MatchGameEvent Event { get; set; }
        public bool IsPoint { get; set; }
        public bool IsValid { get; set; } = true;
    }
}
