//Controllers/DashboardController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WellUp.CustomerPortal.Models.ViewModels;
using WellUp.Core.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace WellUp.CustomerPortal.Controllers
{
    [Authorize(Policy = "CustomerOnly")]
    public class DashboardController : Controller
    {
        private readonly WellUpDbContext _dbContext;

        public DashboardController(WellUpDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get customer information
            var customer = await _dbContext.Customers
                .Include(c => c.CustomerDetails)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (customer == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Create view model for dashboard
            var viewModel = new CustomerDashboardViewModel
            {
                CustomerName = customer.CustomerDetails.FirstOrDefault()?.CustomerName ?? "Customer",
                Email = customer.CustomerEmail,
            };

            // Redirect to products page
            return RedirectToAction("Index", "Products");
        }
    }
}