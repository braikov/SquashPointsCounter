namespace Squash.Web.Areas.Public.Models
{
    public class DashboardViewModel : PageViewModel
    {
        public string PlayerName { get; set; } = string.Empty;
        public string Initials { get; set; } = "??";
        public string? ImaId { get; set; }
        public string? EsfId { get; set; }
        public string? RankedinId { get; set; }
        public string? CountryName { get; set; }
        public string? CountryFlagUrl { get; set; }
        public int? Age { get; set; }
        public string? PictureUrl { get; set; }
        public DashboardStatsViewModel Stats { get; set; } = new();
        public List<DashboardMatchViewModel> RecentMatches { get; set; } = new();
    }

    public class DashboardStatsViewModel
    {
        public int CareerWins { get; set; }
        public int CareerLosses { get; set; }
        public int YearWins { get; set; }
        public int YearLosses { get; set; }
        public List<string> FormResults { get; set; } = new();

        public int CareerTotal => CareerWins + CareerLosses;
        public int YearTotal => YearWins + YearLosses;
        public int CareerWinPercent => CareerTotal == 0 ? 0 : (int)Math.Round(100.0 * CareerWins / CareerTotal);
        public int YearWinPercent => YearTotal == 0 ? 0 : (int)Math.Round(100.0 * YearWins / YearTotal);
    }

    public class DashboardMatchViewModel
    {
        public string OpponentName { get; set; } = string.Empty;
        public string TournamentName { get; set; } = string.Empty;
        public DateTime? MatchDate { get; set; }
        public string ResultLabel { get; set; } = string.Empty;
        public string? Score { get; set; }
    }
}
