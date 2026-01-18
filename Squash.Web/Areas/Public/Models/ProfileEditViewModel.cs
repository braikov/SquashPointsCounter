using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Squash.Web.Resources.Shared;

namespace Squash.Web.Areas.Public.Models
{
    public class ProfileEditViewModel : PageViewModel
    {
        public string Culture { get; set; } = "en";

        [Required(ErrorMessageResourceType = typeof(Validation), ErrorMessageResourceName = "Required")]
        [EmailAddress(ErrorMessageResourceType = typeof(Validation), ErrorMessageResourceName = "InvalidEmail")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessageResourceType = typeof(Validation), ErrorMessageResourceName = "PasswordMismatch")]
        [Display(Name = "Confirm password")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessageResourceType = typeof(Validation), ErrorMessageResourceName = "Required")]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Validation), ErrorMessageResourceName = "Required")]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Validation), ErrorMessageResourceName = "Required")]
        [DataType(DataType.Date)]
        [Display(Name = "Birth date")]
        public DateTime? BirthDate { get; set; }

        [Required(ErrorMessageResourceType = typeof(Validation), ErrorMessageResourceName = "Required")]
        [Display(Name = "Gender")]
        public string Gender { get; set; } = string.Empty;

        [Required(ErrorMessageResourceType = typeof(Validation), ErrorMessageResourceName = "Required")]
        [Range(1, int.MaxValue, ErrorMessageResourceType = typeof(Validation), ErrorMessageResourceName = "SelectOption")]
        [Display(Name = "Country")]
        public int CountryId { get; set; }

        [Required(ErrorMessageResourceType = typeof(Validation), ErrorMessageResourceName = "Required")]
        [Display(Name = "Sport")]
        public string Sport { get; set; } = "Squash";

        public List<SelectListItem> Countries { get; set; } = new();
        public List<SelectListItem> Genders { get; set; } = new();
        public List<SelectListItem> Sports { get; set; } = new();

        public bool SaveSucceeded { get; set; }

        public string? CurrentAvatarUrl { get; set; }
        public string AvatarInitials { get; set; } = "??";
        public Microsoft.AspNetCore.Http.IFormFile? AvatarFile { get; set; }
    }
}
