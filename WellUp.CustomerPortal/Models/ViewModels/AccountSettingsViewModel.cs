// WellUp.CustomerPortal.Models.ViewModels/AccountSettingsViewModel.cs

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WellUp.Core.Database;

namespace WellUp.CustomerPortal.Models.ViewModels
{
    public class AccountSettingsViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public List<Address> Addresses { get; set; } = new List<Address>();
    }

    public class ProfileViewModel
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string CustomerName { get; set; }

        [Display(Name = "Email Address")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } // Read-only

        [Required(ErrorMessage = "Phone number is required")]
        [Display(Name = "Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Phone number must be between 10 and 15 digits")]
        public string Phone { get; set; }
    }

    public class AddressViewModel
    {
        public int AddressId { get; set; }

        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Street address is required")]
        [Display(Name = "Street")]
        [StringLength(255, ErrorMessage = "Street address cannot exceed 255 characters")]
        public string Street { get; set; }

        [Required(ErrorMessage = "Barangay is required")]
        [Display(Name = "Barangay")]
        [StringLength(100, ErrorMessage = "Barangay cannot exceed 100 characters")]
        public string Barangay { get; set; }

        [Required(ErrorMessage = "City/Municipality is required")]
        [Display(Name = "City/Municipality")]
        [StringLength(100, ErrorMessage = "City/Municipality cannot exceed 100 characters")]
        public string CityMunicipality { get; set; }

        [Required(ErrorMessage = "ZIP code is required")]
        [Display(Name = "ZIP Code")]
        [RegularExpression(@"^\d{4,5}$", ErrorMessage = "Invalid ZIP code")]
        [StringLength(10, ErrorMessage = "ZIP code cannot exceed 10 characters")]
        public string ZipCode { get; set; }

        [Display(Name = "Set as Default Address")]
        public bool IsDefault { get; set; }
    }
}