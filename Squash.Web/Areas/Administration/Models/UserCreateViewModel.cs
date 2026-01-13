using System.ComponentModel.DataAnnotations;

namespace Squash.Web.Areas.Administration.Models
{
    public class UserCreateViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public required string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public required string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password))]
        public required string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "Name")]
        public required string Name { get; set; }

        [Required]
        [Display(Name = "Phone")]
        public required string Phone { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Birth date")]
        public DateTime? BirthDate { get; set; }

        [Required]
        [Display(Name = "ZIP")]
        public required string Zip { get; set; }

        [Required]
        [Display(Name = "City")]
        public required string City { get; set; }

        [Required]
        [Display(Name = "Address")]
        public required string Address { get; set; }
    }
}
