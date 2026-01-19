namespace Squash.Web.Areas.Public.Models
{
    public class MatchesIndexViewModel : PageViewModel
    {
        public string Culture { get; set; } = "en";
        
        public List<MatchListItemViewModel> UpcomingMatches { get; set; } = new();
        public int UpcomingMatchesCount { get; set; }
        public int UpcomingCurrentPage { get; set; } = 1;
        public int UpcomingTotalPages { get; set; } = 1;
        
        public List<MatchListItemViewModel> PastMatches { get; set; } = new();
        public int PastMatchesCount { get; set; }
        public int PastCurrentPage { get; set; } = 1;
        public int PastTotalPages { get; set; } = 1;
        
        public int PageSize { get; set; } = 10;
    }

    public class MatchListItemViewModel
    {
        public int Id { get; set; }
        public string Player1Name { get; set; } = string.Empty;
        public string? Player1FlagUrl { get; set; }
        public string Player2Name { get; set; } = string.Empty;
        public string? Player2FlagUrl { get; set; }
        public bool Player1IsWinner { get; set; }
        public bool Player2IsWinner { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public string? TournamentFlagUrl { get; set; }
        public DateTime? MatchDate { get; set; }
        public TimeSpan? StartTime { get; set; }
        public string? ScoreLine { get; set; }
        public string? DrawName { get; set; }
        public string? RoundName { get; set; }
        public string? CourtName { get; set; }
    }
}
