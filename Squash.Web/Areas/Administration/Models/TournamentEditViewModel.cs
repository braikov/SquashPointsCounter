using System;
using System.ComponentModel.DataAnnotations;

namespace Squash.Web.Areas.Administration.Models
{
    public class TournamentEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string? Name { get; set; }

        [Display(Name = "Start date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Closing sign-in date")]
        [DataType(DataType.Date)]
        public DateTime? ClosingSigninDate { get; set; }

        [Display(Name = "Regulations")]
        public string? Regulations { get; set; }
    }
}
