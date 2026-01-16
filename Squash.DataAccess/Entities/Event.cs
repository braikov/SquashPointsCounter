namespace Squash.DataAccess.Entities
{
    public class Event : Squash.DataAccess.Entities.EntityBase
    {
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null!;

        public int? ExternalEventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public MatchType MatchType { get; set; }
        public int Age { get; set; }
        public Direction Direction { get; set; }
    }

    public enum MatchType
    {
        MenSingles = 0,
        WomenSingles = 1,
        MenDoubles = 2,
        WomenDoubles = 3,
        MixedDoubles = 4
    }

    public enum Direction
    {
        Under = 0,
        Above = 1
    }

    public enum EventAge
    {
        Under7 = 0,
        Under9 = 1,
        Under11 = 2,
        Under13 = 3,
        Under15 = 4,
        Under17 = 5,
        Under19 = 6,
        Under21 = 7,
        Above25 = 8,
        Above30 = 9,
        Above35 = 10,
        Above40 = 11,
        Above45 = 12,
        Above50 = 13,
        Above55 = 14,
        Above60 = 15,
        Above65 = 16,
        Above70 = 17,
        Above75 = 18,
        Above80 = 19
    }
}
