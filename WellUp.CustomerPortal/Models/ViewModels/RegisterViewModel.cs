using System.ComponentModel.DataAnnotations;

namespace WellUp.CustomerPortal.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Phone number must be between 10 and 15 digits")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 8)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Street address is required")]
        [Display(Name = "Street")]
        public string Street { get; set; }

        [Required(ErrorMessage = "Barangay is required")]
        [Display(Name = "Barangay")]
        public string Barangay { get; set; }

        [Required(ErrorMessage = "City/Municipality is required")]
        [Display(Name = "City/Municipality")]
        public string CityMunicipality { get; set; }

        [Required(ErrorMessage = "ZIP code is required")]
        [Display(Name = "ZIP Code")]
        [RegularExpression(@"^\d{4,5}$", ErrorMessage = "Invalid ZIP code")]
        public string ZipCode { get; set; }

        [Required(ErrorMessage = "You must agree to the Terms and Conditions")]
        [Display(Name = "I agree to the Terms and Conditions")]
        public bool AgreeToTerms { get; set; }
    }
}