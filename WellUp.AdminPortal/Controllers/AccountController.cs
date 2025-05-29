// AccountController.cs

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WellUp.AdminPortal.Models;
using WellUp.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WellUp.Core.Database;

namespace WellUp.AdminPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly WellUpDbContext _dbContext;
        private readonly ILogger<AccountController> _logger;

        public AccountController(WellUpDbContext context, ILogger<AccountController> logger)
        {
            _dbContext = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If user is already logged in, redirect to dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find the admin in the database
            var admin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Email == model.Email);

            if (admin == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            // Verify password
            var passwordHasher = new PasswordHasher<Admin>();
            var result = passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, model.Password);

            if (result == PasswordVerificationResult.Success)
            {
                // Create the claims for the admin
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, admin.Email),
                    new Claim(ClaimTypes.Role, "Administrator"),
                    new Claim("AdminId", admin.AdminId.ToString()),
                    new Claim("AdminName", admin.AdminName ?? "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                // Sign in the admin
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation($"Admin {model.Email} logged in successfully at {DateTime.UtcNow}");

                // Add logging to verify the claims being created
                _logger.LogInformation($"Claims created: {string.Join(", ", claims.Select(c => $"{c.Type}={c.Value}"))}");

                return RedirectToAction("Index", "Dashboard");
            }

            // If we got this far, something failed
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            _logger.LogWarning($"Access denied to: {returnUrl}");
            _logger.LogInformation($"User authenticated: {User.Identity?.IsAuthenticated}");
            _logger.LogInformation($"User name: {User.Identity?.Name}");
            _logger.LogInformation($"User claims: {string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}"))}");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
    }
}