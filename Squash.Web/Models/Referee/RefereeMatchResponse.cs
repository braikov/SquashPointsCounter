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
        public int GameScoreFirst { get; set; }
        public int GameScoreSecond { get; set; }
    }

    public class RefereePlayer
    {
        public string Name { get; set; } = string.Empty;
        public string? PictureUrl { get; set; }
        public string Nationality { get; set; } = string.Empty;
        public string NationalityFlagUrl { get; set; } = string.Empty;
    }
}
