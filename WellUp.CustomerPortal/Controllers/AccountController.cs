using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            ViewData["ReturnUrl"] = model.ReturnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Find customer by email
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerEmail == model.Email);

                if (customer == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
                    return View(model);
                }

                // Verify password
                var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Customer>();
                var result = passwordHasher.VerifyHashedPassword(customer, customer.PasswordHash, model.Password);

                if (result == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success)
                {
                    // Get customer details
                    var details = await _context.CustomerDetails
                        .FirstOrDefaultAsync(d => d.CustomerId == customer.CustomerId);

                    // Create the identity
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, details?.CustomerName ?? "Customer"),
                        new Claim(ClaimTypes.Email, customer.CustomerEmail),
                        new Claim(ClaimTypes.Role, "Customer"),
                        new Claim("CustomerId", customer.CustomerId.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };

                    // Sign in the user
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    _logger.LogInformation($"Customer {model.Email} logged in successfully");

                    // Redirect to returnUrl or default
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process");
            }

            // If we got this far, something failed
            ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your email and password.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
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
                if (await _context.Customers.AnyAsync(c => c.CustomerEmail == model.Email))
                {
                    ModelState.AddModelError("Email", "This email is already registered.");
                    return View(model);
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Create new customer
                    var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Customer>();
                    var customer = new Customer
                    {
                        CustomerEmail = model.Email,
                        DateCreated = DateTime.Now
                    };

                    // Hash password
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
                    var address = new WellUp.Core.Database.Address
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

                    // Log success
                    _logger.LogInformation($"User {model.Email} registered successfully");

                    // Set success message for login page
                    TempData["SuccessMessage"] = "Registration successful! You can now log in with your new account.";

                    // Redirect to login page
                    return RedirectToAction(nameof(Login));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // Log the full exception details including inner exceptions
                    _logger.LogError(ex, "Error during customer registration: {Message}", ex.Message);

                    // Show the actual error message without checking environment
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
            return RedirectToAction("Index", "Home");
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
                .FirstOrDefaultAsync(c => c.CustomerEmail == model.Email);

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
                    .FirstOrDefaultAsync(c => c.CustomerEmail == model.Email);

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
                    .FirstOrDefaultAsync(c => c.CustomerEmail == model.Email);

                if (customer == null)
                {
                    ModelState.AddModelError(string.Empty, "Unable to reset password. Please try again.");
                    return View(model);
                }

                // Update the password
                var passwordHasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Customer>();
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
    }
}