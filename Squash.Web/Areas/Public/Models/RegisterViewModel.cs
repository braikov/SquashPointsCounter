using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Squash.Web.Areas.Public.Models
{
    public class RegisterViewModel : PageViewModel
    {
        public string Culture { get; set; } = "en";
        public string? ReturnUrl { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "Required")]
        [EmailAddress(ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "InvalidEmail")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "Required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "Required")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "PasswordMismatch")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "Required")]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "Required")]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "Required")]
        [DataType(DataType.Date)]
        [Display(Name = "Birth date")]
        public DateTime? BirthDate { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "Required")]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "Required")]
        [Display(Name = "Country")]
        [Range(1, int.MaxValue, ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "SelectOption")]
        public int CountryId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "Required")]
        [Display(Name = "Sport")]
        public string Sport { get; set; } = "Squash";

        public List<SelectListItem> Countries { get; set; } = new();
        public List<SelectListItem> Genders { get; set; } = new();
        public List<SelectListItem> Sports { get; set; } = new();
    }
}
