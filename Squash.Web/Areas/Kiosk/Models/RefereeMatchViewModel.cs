namespace Squash.Web.Areas.Kiosk.Models
{
    public class RefereeMatchViewModel
    {
        public string Draw { get; set; } = string.Empty;
        public string Court { get; set; } = string.Empty;
        public RefereePlayerViewModel FirstPlayer { get; set; } = new();
        public RefereePlayerViewModel SecondPlayer { get; set; } = new();
        public int MatchGameId { get; set; }
        public string GameScore { get; set; } = "0:0";
        public string CurrentGameScore { get; set; } = "0:0";
    }

    public class RefereePlayerViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? PictureUrl { get; set; }
        public string Nationality { get; set; } = string.Empty;
        public string NationalityFlagUrl { get; set; } = string.Empty;
    }
}
