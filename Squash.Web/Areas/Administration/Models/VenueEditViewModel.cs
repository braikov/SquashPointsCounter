using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Squash.Web.Areas.Administration.Models
{
    public class VenueEditViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string? Name { get; set; }

        [Display(Name = "Street")]
        public string? Street { get; set; }

        [Display(Name = "City")]
        public string? City { get; set; }

        [Display(Name = "Zip")]
        public string? Zip { get; set; }

        [Display(Name = "Region")]
        public string? Region { get; set; }

        [Display(Name = "Country")]
        public int? CountryId { get; set; }

        [Display(Name = "Latitude")]
        public double? Latitude { get; set; }

        [Display(Name = "Longitude")]
        public double? Longitude { get; set; }

        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Website")]
        public string? Website { get; set; }

        public List<SelectListItem> AvailableCountries { get; set; } = new();
    }
}
