// WellUp.CustomerPortal.Models.ViewModels/ForgotPasswordViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace WellUp.CustomerPortal.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }
    }
}