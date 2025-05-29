//SettingsViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace WellUp.AdminPortal.Models.ViewModels
{
    public class SecuritySettingsViewModel
    {
        public int AdminId { get; set; }

        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Display(Name = "Admin Name")]
        public string? AdminName { get; set; }
    }

    public class ChangePasswordViewModel
    {
        public int AdminId { get; set; }

        [Required(ErrorMessage = "Current password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class BackupFileViewModel
    {
        public string FileName { get; set; }
        public DateTime CreatedDate { get; set; }
        public long FileSizeKB { get; set; }
    }

    public class SystemInfoViewModel
    {
        public string DatabaseServerVersion { get; set; }
        public string DatabaseSize { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public string OperatingSystem { get; set; }
        public DateTime ServerTime { get; set; }
        public string FrameworkVersion { get; set; }
        public DateTime? LastBackupDate { get; set; }
    }

    public class DeleteBackupViewModel
    {
        public string FileName { get; set; }

        [Required(ErrorMessage = "Password is required to confirm deletion")]
        [DataType(DataType.Password)]
        [Display(Name = "Admin Password")]
        public string Password { get; set; }
    }

    public class RestoreBackupViewModel
    {
        public string FileName { get; set; }

        [Required(ErrorMessage = "Password is required to confirm database restoration")]
        [DataType(DataType.Password)]
        [Display(Name = "Admin Password")]
        public string Password { get; set; }
    }

    public class ActivityLogViewModel
    {
        public int LogId { get; set; }
        public string AdminName { get; set; }
        public string Activity { get; set; }
        public string Details { get; set; }
        public string IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }
}