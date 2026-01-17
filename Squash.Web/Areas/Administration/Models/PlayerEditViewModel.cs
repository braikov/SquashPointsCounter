using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Squash.Web.Areas.Administration.Models
{
    public class PlayerEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string? Name { get; set; }

        [Display(Name = "Country")]
        public int? CountryId { get; set; }

        [Display(Name = "ESF member id")]
        public string? EsfMemberId { get; set; }

        public List<SelectListItem> AvailableCountries { get; set; } = new();
    }
}

