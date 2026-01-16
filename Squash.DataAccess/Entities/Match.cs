using Squash.DataAccess.Utils;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;

namespace Squash.DataAccess.Entities
{
    public class Match : Squash.DataAccess.Entities.EntityBase
    {
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null!;

        public int? TournamentDayId { get; set; }
        public TournamentDay? TournamentDay { get; set; }

        public int? DrawId { get; set; }
        public Draw? Draw { get; set; }

        public int? RoundId { get; set; }
        public Round? Round { get; set; }

        public int? CourtId { get; set; }
        public Court? Court { get; set; }

        public int? Player1Id { get; set; }
        public Player? Player1 { get; set; }

        public int? Player2Id { get; set; }
        public Player? Player2 { get; set; }

        public int? WinnerPlayerId { get; set; }
        public Player? WinnerPlayer { get; set; }

        public TimeSpan? StartTime { get; set; }
        public string? StartTimeText { get; set; }
        public string? Status { get; set; }
        public string? HeadToHeadUrl { get; set; }

        public ICollection<MatchGame> Games { get; set; } = new List<MatchGame>();

        public string PinCode { get; set; } = RandomCodeGenerator.GenerateSixCharCode();

        [NotMapped]
        public string MatchKey
        {
            get
            {
                var datePart = TournamentDay?.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "00000000";
                var timePart = StartTime?.ToString(@"hh\:mm", CultureInfo.InvariantCulture) ?? (StartTimeText ?? string.Empty).Trim();
                var side1 = Player1?.Id ?? 0;
                var side2 = Player2?.Id ?? 0;
                return $"{datePart}|{timePart}|{side1}|{side2}";
            }
        }
    }
}
