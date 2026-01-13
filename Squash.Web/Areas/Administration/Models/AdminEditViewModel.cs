using System.ComponentModel.DataAnnotations;

namespace Squash.Web.Areas.Administration.Models
{
    public class AdminEditViewModel
    {
        [Required]
        public required string Id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        [StringLength(256)]
        public required string Email { get; set; }

        [Display(Name = "Locked")]
        public bool Locked { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string? ConfirmNewPassword { get; set; }
    }
}
