using System;
using System.Collections.Generic;

namespace Squash.Web.Areas.Administration.Models
{
    public class TournamentDayScheduleViewModel
    {
        public int TournamentId { get; set; }
        public string TournamentName { get; set; } = string.Empty;
        public int? SelectedDayId { get; set; }
        public DateTime? SelectedDate { get; set; }
        public List<TournamentDayTabViewModel> Days { get; set; } = new();
        public List<TournamentMatchRowViewModel> Matches { get; set; } = new();

        // Filters
        public string? FilterCountry { get; set; }
        public string? FilterDraw { get; set; }
        public string? FilterRound { get; set; }
        public string? FilterCourt { get; set; }

        // Available filter options
        public List<FilterOption> AvailableCountries { get; set; } = new();
        public List<FilterOption> AvailableDraws { get; set; } = new();
        public List<FilterOption> AvailableRounds { get; set; } = new();
        public List<FilterOption> AvailableCourts { get; set; } = new();

        // Total count before filtering
        public int TotalMatchesCount { get; set; }
        public int FilteredMatchesCount { get; set; }
    }

    public class FilterOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class TournamentDayTabViewModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public bool IsSelected { get; set; }
    }

    public class TournamentMatchRowViewModel
    {
        public string Time { get; set; } = string.Empty;
        public string Draw { get; set; } = string.Empty;
        public string Round { get; set; } = string.Empty;
        public string Court { get; set; } = string.Empty;
        public string Player1 { get; set; } = string.Empty;
        public string Player2 { get; set; } = string.Empty;
        public string Player1FlagUrl { get; set; } = string.Empty;
        public string Player2FlagUrl { get; set; } = string.Empty;
        public string Player1CountryCode { get; set; } = string.Empty;
        public string Player2CountryCode { get; set; } = string.Empty;
        public bool Player1IsWinner { get; set; }
        public bool Player2IsWinner { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
        public bool IsFinished { get; set; }
        public List<MatchGameScoreViewModel> Games { get; set; } = new();
    }

    public class MatchGameScoreViewModel
    {
        public int GameNumber { get; set; }
        public int? Side1Points { get; set; }
        public int? Side2Points { get; set; }
        public int? WinnerSide { get; set; }
    }
}
