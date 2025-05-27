// WellUp.CustomerPortal.Models.ViewModels/VerifyIdentityViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace WellUp.CustomerPortal.Models.ViewModels
{
    public class VerifyIdentityViewModel
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Phone number must be between 10 and 15 digits")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "ZIP code is required")]
        [Display(Name = "ZIP Code")]
        [RegularExpression(@"^\d{4,5}$", ErrorMessage = "Invalid ZIP code")]
        public string ZipCode { get; set; }
    }
}