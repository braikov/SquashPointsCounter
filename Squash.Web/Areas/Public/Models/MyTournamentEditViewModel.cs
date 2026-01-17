using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Squash.Web.Resources.Shared;

namespace Squash.Web.Areas.Public.Models
{
    public class MyTournamentEditViewModel : PageViewModel
    {
        public int Id { get; set; }
        public string Culture { get; set; } = "en";

        [Required(ErrorMessageResourceType = typeof(Validation), ErrorMessageResourceName = "Required")]
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Validation), ErrorMessageResourceName = "Required")]
        [Range(1, int.MaxValue, ErrorMessageResourceType = typeof(Validation), ErrorMessageResourceName = "SelectOption")]
        [Display(Name = "Country")]
        public int CountryId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Start date")]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "End date")]
        public DateTime? EndDate { get; set; }

        public List<SelectListItem> Countries { get; set; } = new();
    }
}
