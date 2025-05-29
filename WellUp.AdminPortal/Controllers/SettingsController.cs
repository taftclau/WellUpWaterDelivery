//SettingsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using WellUp.AdminPortal.Models.ViewModels;
using WellUp.Core.Data;
using WellUp.Core.Database;

namespace WellUp.AdminPortal.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class SettingsController : Controller
    {
        private readonly WellUpDbContext _dbContext;
        private readonly ILogger<SettingsController> _logger;
        private readonly IConfiguration _configuration;

        public SettingsController(WellUpDbContext dbContext, ILogger<SettingsController> logger, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _logger = logger;
            _configuration = configuration;
        }

        // GET: Settings
        public IActionResult Index()
        {
            return View();
        }

        // GET: Settings/Security
        public async Task<IActionResult> Security()
        {
            // Get current admin ID from claims
            var adminIdClaim = User.FindFirst("AdminId")?.Value;
            if (string.IsNullOrEmpty(adminIdClaim) || !int.TryParse(adminIdClaim, out int adminId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Get admin info
            var admin = await _dbContext.Admins.FindAsync(adminId);
            if (admin == null)
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var viewModel = new SecuritySettingsViewModel
            {
                AdminId = admin.AdminId,
                Email = admin.Email,
                AdminName = admin.AdminName
            };

            return View(viewModel);
        }

        // POST: Settings/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            // Create the view model that will be returned to the view
            var viewModel = new SecuritySettingsViewModel
            {
                AdminId = model.AdminId,
                Email = await _dbContext.Admins
                    .Where(a => a.AdminId == model.AdminId)
                    .Select(a => a.Email)
                    .FirstOrDefaultAsync(),
                AdminName = await _dbContext.Admins
                    .Where(a => a.AdminId == model.AdminId)
                    .Select(a => a.AdminName)
                    .FirstOrDefaultAsync()
            };

            // Check model validity first
            if (!ModelState.IsValid)
            {
                // Store the error messages in TempData to be retrieved and displayed in the view
                foreach (var key in ModelState.Keys)
                {
                    if (ModelState[key].Errors.Count > 0)
                    {
                        TempData[key + "_Error"] = ModelState[key].Errors.FirstOrDefault()?.ErrorMessage;
                    }
                }

                return View("Security", viewModel);
            }

            // Get admin details
            var admin = await _dbContext.Admins.FindAsync(model.AdminId);
            if (admin == null)
            {
                TempData["ErrorMessage"] = "Admin account not found.";
                return View("Security", viewModel);
            }

            // Verify old password
            var passwordHasher = new PasswordHasher<Admin>();
            var verifyResult = passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, model.CurrentPassword);
            if (verifyResult != PasswordVerificationResult.Success)
            {
                TempData["CurrentPassword_Error"] = "The current password is incorrect.";
                return View("Security", viewModel);
            }

            // Validate new password strength
            var passwordErrors = ValidatePasswordStrength(model.NewPassword);
            if (passwordErrors.Count > 0)
            {
                TempData["NewPassword_Error"] = string.Join(" ", passwordErrors);
                return View("Security", viewModel);
            }

            // Ensure passwords match
            if (model.NewPassword != model.ConfirmPassword)
            {
                TempData["ConfirmPassword_Error"] = "The new password and confirmation password do not match.";
                return View("Security", viewModel);
            }

            // Hash new password and update
            admin.PasswordHash = passwordHasher.HashPassword(admin, model.NewPassword);
            admin.DateModified = DateTime.Now;

            _dbContext.Update(admin);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Admin ID {admin.AdminId} password changed successfully");

            // Set success message
            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction(nameof(Security));
        }

        private List<string> ValidatePasswordStrength(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("Password cannot be empty.");
                return errors;
            }

            if (password.Length < 8)
                errors.Add("Password must be at least 8 characters long.");

            if (!password.Any(char.IsUpper))
                errors.Add("Password must contain at least one uppercase letter.");

            if (!password.Any(char.IsLower))
                errors.Add("Password must contain at least one lowercase letter.");

            if (!password.Any(char.IsDigit))
                errors.Add("Password must contain at least one number.");

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
                errors.Add("Password must contain at least one special character.");

            return errors;
        }

        // GET: Settings/DataManagement
        public IActionResult DataManagement()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackupDatabase()
        {
            try
            {
                // Create a timestamp for the backup file
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");

                // Ensure directory exists
                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                string backupFileName = $"WellUpDB_Backup_{timestamp}.bak";
                string backupFilePath = Path.Combine(backupDirectory, backupFileName);

                // In a real application, this would execute a SQL backup command
                // For demo purposes, we'll just simulate a backup by creating a file
                await Task.Delay(2000); // Simulate backup process time

                // Create an empty file just for simulation
                using (FileStream fs = System.IO.File.Create(backupFilePath))
                {
                    // Write some dummy content to the file
                    byte[] dummyContent = new byte[] { 0x01, 0x02, 0x03, 0x04 };
                    await fs.WriteAsync(dummyContent, 0, dummyContent.Length);
                }

                _logger.LogInformation($"Database backup created: {backupFileName}");

                // Set success message
                TempData["SuccessMessage"] = $"Database backup created successfully: {backupFileName}";
                return RedirectToAction(nameof(DataManagement));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating database backup: {ex.Message}");
                TempData["ErrorMessage"] = "Error creating database backup. Please try again.";
                return RedirectToAction(nameof(DataManagement));
            }
        }

        // GET: Settings/BackupHistory
        public IActionResult BackupHistory()
        {
            try
            {
                string backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");

                var backupFiles = new List<BackupFileViewModel>();

                // Check if directory exists
                if (Directory.Exists(backupDirectory))
                {
                    // Get all backup files
                    var files = Directory.GetFiles(backupDirectory, "WellUpDB_Backup_*.bak");

                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        backupFiles.Add(new BackupFileViewModel
                        {
                            FileName = fileInfo.Name,
                            CreatedDate = fileInfo.CreationTime,
                            FileSizeKB = fileInfo.Length / 1024
                        });
                    }
                }

                return View(backupFiles.OrderByDescending(f => f.CreatedDate).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving backup history: {ex.Message}");
                TempData["ErrorMessage"] = "Error retrieving backup history.";
                return RedirectToAction(nameof(DataManagement));
            }
        }

        [HttpGet]
        public IActionResult DownloadBackup(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    TempData["ErrorMessage"] = "File name is required.";
                    return RedirectToAction(nameof(BackupHistory));
                }

                string backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
                string filePath = Path.Combine(backupDirectory, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "Backup file not found.";
                    return RedirectToAction(nameof(BackupHistory));
                }

                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading backup: {ex.Message}");
                TempData["ErrorMessage"] = "Error downloading backup file.";
                return RedirectToAction(nameof(BackupHistory));
            }
        }

        // GET: Settings/DeleteBackupConfirm
        public IActionResult DeleteBackupConfirm(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                TempData["ErrorMessage"] = "File name is required.";
                return RedirectToAction(nameof(BackupHistory));
            }

            var viewModel = new DeleteBackupViewModel
            {
                FileName = fileName
            };

            return View(viewModel);
        }

        // POST: Settings/DeleteBackup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBackup(DeleteBackupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("DeleteBackupConfirm", model);
            }

            // Get admin ID from claims
            var adminIdClaim = User.FindFirst("AdminId")?.Value;
            if (string.IsNullOrEmpty(adminIdClaim) || !int.TryParse(adminIdClaim, out int adminId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Get admin and verify password
            var admin = await _dbContext.Admins.FindAsync(adminId);
            if (admin == null)
            {
                ModelState.AddModelError(string.Empty, "Admin account not found.");
                return View("DeleteBackupConfirm", model);
            }

            // Verify password
            var passwordHasher = new PasswordHasher<Admin>();
            var verifyResult = passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, model.Password);
            if (verifyResult != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("Password", "Password is incorrect.");
                return View("DeleteBackupConfirm", model);
            }

            try
            {
                string backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
                string filePath = Path.Combine(backupDirectory, model.FileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    TempData["SuccessMessage"] = "Backup file deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Backup file not found.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting backup: {ex.Message}");
                TempData["ErrorMessage"] = "Error deleting backup file.";
            }

            return RedirectToAction(nameof(BackupHistory));
        }

        // GET: Settings/RestoreBackupConfirm
        public IActionResult RestoreBackupConfirm(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                TempData["ErrorMessage"] = "File name is required.";
                return RedirectToAction(nameof(BackupHistory));
            }

            var viewModel = new RestoreBackupViewModel
            {
                FileName = fileName
            };

            return View(viewModel);
        }

        // POST: Settings/RestoreBackup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreBackup(RestoreBackupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("RestoreBackupConfirm", model);
            }

            // Get admin ID from claims
            var adminIdClaim = User.FindFirst("AdminId")?.Value;
            if (string.IsNullOrEmpty(adminIdClaim) || !int.TryParse(adminIdClaim, out int adminId))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            // Get admin and verify password
            var admin = await _dbContext.Admins.FindAsync(adminId);
            if (admin == null)
            {
                ModelState.AddModelError(string.Empty, "Admin account not found.");
                return View("RestoreBackupConfirm", model);
            }

            // Verify password
            var passwordHasher = new PasswordHasher<Admin>();
            var verifyResult = passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, model.Password);
            if (verifyResult != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("Password", "Password is incorrect.");
                return View("RestoreBackupConfirm", model);
            }

            try
            {
                string backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
                string filePath = Path.Combine(backupDirectory, model.FileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "Backup file not found.";
                    return RedirectToAction(nameof(BackupHistory));
                }

                // Get the connection string
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                // Create a SQL Server restore
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Get the database name from the connection string
                    var builder = new SqlConnectionStringBuilder(connectionString);
                    string databaseName = builder.InitialCatalog;

                    if (string.IsNullOrEmpty(databaseName))
                    {
                        throw new Exception("Database name could not be determined from connection string");
                    }

                    // First, set the database to single user mode
                    string singleUserQuery = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                    using (var command = new SqlCommand(singleUserQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }

                    try
                    {
                        // Restore the database
                        string restoreQuery = $"RESTORE DATABASE [{databaseName}] FROM DISK = N'{filePath}' WITH FILE = 1, REPLACE, STATS = 10";
                        using (var command = new SqlCommand(restoreQuery, connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }

                        // Set the database back to multi user mode
                        string multiUserQuery = $"ALTER DATABASE [{databaseName}] SET MULTI_USER";
                        using (var command = new SqlCommand(multiUserQuery, connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception)
                    {
                        // If restore fails, make sure we try to set the database back to multi user mode
                        try
                        {
                            string multiUserQuery = $"ALTER DATABASE [{databaseName}] SET MULTI_USER";
                            using (var command = new SqlCommand(multiUserQuery, connection))
                            {
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                        catch
                        {
                            // Just log, don't prevent the main error from being thrown
                        }

                        throw;
                    }
                }

                TempData["SuccessMessage"] = "Database restored successfully from backup.";
                return RedirectToAction(nameof(DataManagement));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error restoring backup: {ex.Message}");
                TempData["ErrorMessage"] = $"Error restoring database from backup: {ex.Message}";
                return RedirectToAction(nameof(BackupHistory));
            }
        }

        private DateTime? GetLastBackupDate()
        {
            try
            {
                string backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");

                if (!Directory.Exists(backupDirectory))
                {
                    return null;
                }

                var files = Directory.GetFiles(backupDirectory, "WellUpDB_Backup_*.bak");
                if (files.Length == 0)
                {
                    return null;
                }

                return files.Select(f => new FileInfo(f).CreationTime)
                           .OrderByDescending(d => d)
                           .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}