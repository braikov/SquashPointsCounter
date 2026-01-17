namespace Squash.Web.Models.Referee
{
    public class RefereeMatchResponse
    {
        public bool Success { get; set; }
        public RefereeMatch? Match { get; set; }
    }

    public class RefereeMatch
    {
        public string Draw { get; set; } = string.Empty;
        public string Court { get; set; } = string.Empty;
        public RefereePlayer FirstPlayer { get; set; } = new();
        public RefereePlayer SecondPlayer { get; set; } = new();
        public int MatchGameId { get; set; }
        public int GameScoreFirst { get; set; }
        public int GameScoreSecond { get; set; }
        public int CurrentGameScoreFirst { get; set; }
        public int CurrentGameScoreSecond { get; set; }
        public int GamesToWin { get; set; }
        public List<RefereeMatchEventLog> EventLogs { get; set; } = new();
    }

    public class RefereePlayer
    {
        public string Name { get; set; } = string.Empty;
        public string? PictureUrl { get; set; }
        public string Country { get; set; } = string.Empty;
        public string CountryFlagUrl { get; set; } = string.Empty;
    }

    public class RefereeMatchEventLog
    {
        public long Id { get; set; }
        public string Event { get; set; } = string.Empty;
        public bool IsPoint { get; set; }
        public bool IsValid { get; set; }
    }

    public class GameLogRequest
    {
        public int MatchGameId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int? WinnerSide { get; set; }
    }

    public class GameStartedRequest
    {
        public string? Pin { get; set; }
    }

    public class GameStartedResponse
    {
        public bool Success { get; set; }
        public int MatchGameId { get; set; }
    }
}

