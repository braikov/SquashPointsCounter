namespace Squash.DataAccess.Entities
{
    public class ReferredMatch : EntityBase
    {
        public int MatchId { get; set; }
        public Match Match { get; set; } = null!;

        public string PinCode { get; set; } = string.Empty;
        public List<string> Referee { get; set; } = new();
    }
}
