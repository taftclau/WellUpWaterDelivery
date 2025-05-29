//Controllers/AccountController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using WellUp.Core.Data;
using WellUp.Core.Database;
using WellUp.CustomerPortal.Models.ViewModels;

namespace WellUp.CustomerPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly WellUpDbContext _context;

        public AccountController(ILogger<AccountController> logger, WellUpDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            // If user is already logged in, redirect to products page
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Products");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Store the return URL
            ViewData["ReturnUrl"] = model.ReturnUrl;

            // Remove ModelState validation for ReturnUrl if it's causing problems
            if (!ModelState.IsValid && ModelState.ContainsKey("ReturnUrl"))
            {
                ModelState.Remove("ReturnUrl");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Find the customer (case insensitive)
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerEmail.ToLower() == model.Email.ToLower());

                if (customer == null)
                {
                    _logger.LogWarning($"Login attempt failed: No customer found with email {model.Email}");
                    ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
                    return View(model);
                }

                bool verified = false;
                var passwordHasher = new PasswordHasher<Customer>();
                var result = passwordHasher.VerifyHashedPassword(customer, customer.PasswordHash, model.Password);

                if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    verified = true;

                    // Update hash if needed
                    if (result == PasswordVerificationResult.SuccessRehashNeeded)
                    {
                        customer.PasswordHash = passwordHasher.HashPassword(customer, model.Password);
                        await _context.SaveChangesAsync();
                    }
                }

                if (verified)
                {
                    // Get customer details
                    var customerDetails = await _context.CustomerDetails
                        .FirstOrDefaultAsync(cd => cd.CustomerId == customer.CustomerId);

                    // Create claims
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, customerDetails?.CustomerName ?? customer.CustomerEmail),
                        new Claim(ClaimTypes.Email, customer.CustomerEmail),
                        new Claim(ClaimTypes.Role, "Customer"),
                        new Claim("CustomerId", customer.CustomerId.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                    };

                    // Sign in the user
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        authProperties);

                    _logger.LogInformation($"Login successful for {model.Email}");

                    // Redirect the user
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Products");
                    }
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during login. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Products");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Check if email is already used
                if (await _context.Customers.AnyAsync(c => c.CustomerEmail.ToLower() == model.Email.ToLower()))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create new customer
                    var passwordHasher = new PasswordHasher<Customer>();
                    var customer = new Customer
                    {
                        CustomerEmail = model.Email,
                        DateCreated = DateTime.Now
                    };

                    // Hash password with standard settings
                    customer.PasswordHash = passwordHasher.HashPassword(customer, model.Password);

                    // Add customer to database
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();

                    // Create customer details
                    var customerDetails = new CustomerDetail
                    {
                        CustomerId = customer.CustomerId,
                        CustomerName = model.FullName,
                        Phone = model.Phone
                    };

                    _context.CustomerDetails.Add(customerDetails);
                    await _context.SaveChangesAsync();

                    // Create customer address
                    var address = new Address
                    {
                        CustomerId = customer.CustomerId,
                        Street = model.Street,
                        Barangay = model.Barangay,
                        CityMunicipality = model.CityMunicipality,
                        ZipCode = model.ZipCode,
                        IsDefault = true
                    };

                    _context.Addresses.Add(address);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // Set success message for login page
                    TempData["SuccessMessage"] = "Registration successful! You can now log in with your new account.";

                    // Redirect to login page
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during customer registration: {Message}", ex.Message);

                    string errorDetails = ex.InnerException != null ?
                        $"{ex.Message} → {ex.InnerException.Message}" : ex.Message;
                    ModelState.AddModelError(string.Empty, $"Registration error: {errorDetails}");

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration process");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if the email exists in our database
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerEmail.ToLower() == model.Email.ToLower());

            if (customer == null)
            {
                // Don't reveal that the user does not exist for security
                // Instead, show the same message as if we proceeded successfully
                TempData["InfoMessage"] = "If your email is registered, you will receive instructions to verify your identity.";
                return RedirectToAction(nameof(Login));
            }

            // Store the email in TempData to pass it to the next step
            TempData["ResetEmail"] = model.Email;

            // Redirect to the identity verification page
            return RedirectToAction(nameof(VerifyIdentity));
        }

        [HttpGet]
        public IActionResult VerifyIdentity()
        {
            // Check if we have an email from the previous step
            var email = TempData["ResetEmail"] as string;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Keep the email available for the post action
            TempData.Keep("ResetEmail");

            var model = new VerifyIdentityViewModel
            {
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyIdentity(VerifyIdentityViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Retrieve the email from TempData
            var email = TempData["ResetEmail"] as string;
            if (string.IsNullOrEmpty(email) || email != model.Email)
            {
                ModelState.AddModelError(string.Empty, "Invalid request. Please try again from the beginning.");
                return View(model);
            }

            try
            {
                // Verify the user exists and phone/ZIP match
                var customer = await _context.Customers
                    .Include(c => c.CustomerDetails)
                    .Include(c => c.Addresses.Where(a => a.IsDefault))
                    .FirstOrDefaultAsync(c => c.CustomerEmail.ToLower() == model.Email.ToLower());

                if (customer == null)
                {
                    ModelState.AddModelError(string.Empty, "We couldn't verify your identity. Please check your information.");
                    return View(model);
                }

                // Verify phone number
                var customerDetail = customer.CustomerDetails.FirstOrDefault();
                if (customerDetail == null || customerDetail.Phone != model.Phone)
                {
                    ModelState.AddModelError(string.Empty, "We couldn't verify your identity. Please check your information.");
                    return View(model);
                }

                // Verify ZIP code
                var defaultAddress = customer.Addresses.FirstOrDefault(a => a.IsDefault);
                if (defaultAddress == null || defaultAddress.ZipCode != model.ZipCode)
                {
                    ModelState.AddModelError(string.Empty, "We couldn't verify your identity. Please check your information.");
                    return View(model);
                }

                // Identity verified successfully
                // Store the email for the reset password page
                TempData["VerifiedEmail"] = model.Email;

                // Redirect to reset password page
                return RedirectToAction(nameof(ResetPassword));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during identity verification: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, "An error occurred while verifying your identity. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            // Check if we have a verified email from the previous step
            var email = TempData["VerifiedEmail"] as string;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Keep the email available for the post action
            TempData.Keep("VerifiedEmail");

            var model = new ResetPasswordViewModel
            {
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Retrieve the verified email from TempData
            var email = TempData["VerifiedEmail"] as string;
            if (string.IsNullOrEmpty(email) || email != model.Email)
            {
                ModelState.AddModelError(string.Empty, "Invalid request. Please try again from the beginning.");
                return View(model);
            }

            try
            {
                // Find the customer by email
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerEmail.ToLower() == model.Email.ToLower());

                if (customer == null)
                {
                    ModelState.AddModelError(string.Empty, "Unable to reset password. Please try again.");
                    return View(model);
                }

                // Update the password
                var passwordHasher = new PasswordHasher<Customer>();
                customer.PasswordHash = passwordHasher.HashPassword(customer, model.NewPassword);
                await _context.SaveChangesAsync();

                // Set success message
                TempData["SuccessMessage"] = "Your password has been reset successfully. You can now log in with your new password.";

                // Redirect to login page
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password reset: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, "An error occurred while resetting your password. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get current user email from claims
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(userEmail))
                {
                    ModelState.AddModelError(string.Empty, "Could not identify the current user.");
                    return View(model);
                }

                // Find customer in database
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerEmail.ToLower() == userEmail.ToLower());

                if (customer == null)
                {
                    ModelState.AddModelError(string.Empty, "User account not found.");
                    return View(model);
                }

                // Verify current password
                var passwordHasher = new PasswordHasher<Customer>();
                var verificationResult = passwordHasher.VerifyHashedPassword(customer, customer.PasswordHash, model.CurrentPassword);

                if (verificationResult == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
                    return View(model);
                }

                // Update to new password
                customer.PasswordHash = passwordHasher.HashPassword(customer, model.NewPassword);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your password has been changed successfully.";

                return RedirectToAction("Index", "Products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during password change: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, "An error occurred while changing your password. Please try again.");
                return View(model);
            }
        }

        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> Settings()
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login");
            }

            // Get customer information with details and addresses
            var customer = await _context.Customers
                .Include(c => c.CustomerDetails)
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return NotFound();
            }

            var customerDetail = customer.CustomerDetails.FirstOrDefault();

            // Create view model
            var viewModel = new AccountSettingsViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerName = customerDetail?.CustomerName ?? "Customer",
                Email = customer.CustomerEmail,
                Phone = customerDetail?.Phone ?? "",
                Addresses = customer.Addresses.ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> EditProfile()
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login");
            }

            // Get customer information
            var customer = await _context.Customers
                .Include(c => c.CustomerDetails)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return NotFound();
            }

            var customerDetail = customer.CustomerDetails.FirstOrDefault();

            // Create view model
            var viewModel = new ProfileViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerName = customerDetail?.CustomerName ?? "",
                Email = customer.CustomerEmail,
                Phone = customerDetail?.Phone ?? ""
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> EditProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get current customer ID from claims
                if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
                {
                    return RedirectToAction("Login");
                }

                // Verify this is the correct customer
                if (customerId != model.CustomerId)
                {
                    return Forbid();
                }

                // Get customer details
                var customerDetail = await _context.CustomerDetails
                    .FirstOrDefaultAsync(cd => cd.CustomerId == customerId);

                if (customerDetail == null)
                {
                    // Create new customer details if not exist
                    customerDetail = new CustomerDetail
                    {
                        CustomerId = customerId,
                        CustomerName = model.CustomerName,
                        Phone = model.Phone
                    };
                    _context.CustomerDetails.Add(customerDetail);
                }
                else
                {
                    // Update existing customer details
                    customerDetail.CustomerName = model.CustomerName;
                    customerDetail.Phone = model.Phone;
                }

                await _context.SaveChangesAsync();

                // Update claims if name changed
                var user = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId);

                if (user != null && User.Identity is ClaimsIdentity identity)
                {
                    var nameClaim = identity.FindFirst(ClaimTypes.Name);
                    if (nameClaim != null && nameClaim.Value != model.CustomerName)
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, model.CustomerName),
                            new Claim(ClaimTypes.Email, user.CustomerEmail),
                            new Claim(ClaimTypes.Role, "Customer"),
                            new Claim("CustomerId", customerId.ToString())
                        };

                        var newIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var principal = new ClaimsPrincipal(newIdentity);

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            principal,
                            new AuthenticationProperties { IsPersistent = true });
                    }
                }

                TempData["SuccessMessage"] = "Your profile has been updated successfully.";
                return RedirectToAction(nameof(Settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, "An error occurred while updating your profile. Please try again.");
                return View(model);
            }
        }

        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> ManageAddresses()
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login");
            }

            // Get customer addresses
            var addresses = await _context.Addresses
                .Where(a => a.CustomerId == customerId)
                .OrderByDescending(a => a.IsDefault)
                .ThenBy(a => a.AddressId)
                .ToListAsync();

            return View(addresses);
        }

        [HttpGet]
        [Authorize(Policy = "CustomerOnly")]
        public IActionResult AddAddress()
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login");
            }

            var viewModel = new AddressViewModel
            {
                CustomerId = customerId,
                IsDefault = false
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> AddAddress(AddressViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get current customer ID from claims
                if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
                {
                    return RedirectToAction("Login");
                }

                // Verify this is the correct customer
                if (customerId != model.CustomerId)
                {
                    return Forbid();
                }

                // If this is set as default, update other addresses
                if (model.IsDefault)
                {
                    var addresses = await _context.Addresses
                        .Where(a => a.CustomerId == customerId && a.IsDefault)
                        .ToListAsync();

                    foreach (var address in addresses)
                    {
                        address.IsDefault = false;
                    }
                }

                // Create new address
                var newAddress = new Address
                {
                    CustomerId = customerId,
                    Street = model.Street,
                    Barangay = model.Barangay,
                    CityMunicipality = model.CityMunicipality,
                    ZipCode = model.ZipCode,
                    IsDefault = model.IsDefault
                };

                _context.Addresses.Add(newAddress);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "New address added successfully.";
                return RedirectToAction(nameof(ManageAddresses));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding address: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, "An error occurred while adding your address. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> EditAddress(int id)
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login");
            }

            // Get address
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == id && a.CustomerId == customerId);

            if (address == null)
            {
                return NotFound();
            }

            // Create view model
            var viewModel = new AddressViewModel
            {
                AddressId = address.AddressId,
                CustomerId = address.CustomerId,
                Street = address.Street,
                Barangay = address.Barangay,
                CityMunicipality = address.CityMunicipality,
                ZipCode = address.ZipCode,
                IsDefault = address.IsDefault
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> EditAddress(AddressViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Get current customer ID from claims
                if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
                {
                    return RedirectToAction("Login");
                }

                // Verify this is the correct customer
                if (customerId != model.CustomerId)
                {
                    return Forbid();
                }

                // Get address
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.AddressId == model.AddressId && a.CustomerId == customerId);

                if (address == null)
                {
                    return NotFound();
                }

                // If this is set as default, update other addresses
                if (model.IsDefault && !address.IsDefault)
                {
                    var addresses = await _context.Addresses
                        .Where(a => a.CustomerId == customerId && a.IsDefault)
                        .ToListAsync();

                    foreach (var addr in addresses)
                    {
                        addr.IsDefault = false;
                    }
                }

                // Update address
                address.Street = model.Street;
                address.Barangay = model.Barangay;
                address.CityMunicipality = model.CityMunicipality;
                address.ZipCode = model.ZipCode;
                address.IsDefault = model.IsDefault;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Address updated successfully.";
                return RedirectToAction(nameof(ManageAddresses));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address: {Message}", ex.Message);
                ModelState.AddModelError(string.Empty, "An error occurred while updating your address. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CustomerOnly")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            try
            {
                // Get current customer ID from claims
                if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
                {
                    return RedirectToAction("Login");
                }

                // Get address
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.AddressId == id && a.CustomerId == customerId);

                if (address == null)
                {
                    return NotFound();
                }

                // Check if address is used in any orders
                bool isUsedInOrders = await _context.Orders
                    .AnyAsync(o => o.AddressId == id);

                if (isUsedInOrders)
                {
                    TempData["ErrorMessage"] = "This address cannot be deleted because it is used in orders.";
                    return RedirectToAction(nameof(ManageAddresses));
                }

                // Check if this is the only address
                int addressCount = await _context.Addresses
                    .CountAsync(a => a.CustomerId == customerId);

                if (addressCount <= 1)
                {
                    TempData["ErrorMessage"] = "You cannot delete your only address.";
                    return RedirectToAction(nameof(ManageAddresses));
                }

                // If deleting the default address, set another one as default
                if (address.IsDefault)
                {
                    var newDefaultAddress = await _context.Addresses
                        .FirstOrDefaultAsync(a => a.CustomerId == customerId && a.AddressId != id);

                    if (newDefaultAddress != null)
                    {
                        newDefaultAddress.IsDefault = true;
                    }
                }

                // Delete address
                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Address deleted successfully.";
                return RedirectToAction(nameof(ManageAddresses));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An error occurred while deleting your address. Please try again.";
                return RedirectToAction(nameof(ManageAddresses));
            }
        }
    }
}