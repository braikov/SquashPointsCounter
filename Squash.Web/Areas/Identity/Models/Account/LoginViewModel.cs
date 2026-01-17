using System.ComponentModel.DataAnnotations;

namespace Squash.Web.Areas.Identity.Models.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "Required")]
        [EmailAddress(ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "InvalidEmail")]
        [Display(Name = "Email")]
        public required string Email { get; set; }

        [Required(ErrorMessageResourceType = typeof(Resources.Shared.Validation), ErrorMessageResourceName = "Required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public required string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
