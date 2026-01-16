using System.ComponentModel.DataAnnotations;
using Squash.DataAccess.Entities;
using MatchType = Squash.DataAccess.Entities.MatchType;

namespace Squash.Web.Areas.Administration.Models
{
    public class TournamentEventEditViewModel
    {
        public int TournamentId { get; set; }
        public int? Id { get; set; }

        [Required]
        [Display(Name = "Event name")]
        public string? Name { get; set; }

        [Display(Name = "Match type")]
        public MatchType MatchType { get; set; } = MatchType.MenSingles;

        public bool UsePresetAge { get; set; } = true;

        [Display(Name = "Age preset")]
        public EventAge? AgePreset { get; set; }

        [Display(Name = "Age")]
        public int? CustomAge { get; set; }

        [Display(Name = "Direction")]
        public Direction? Direction { get; set; }
    }
}
