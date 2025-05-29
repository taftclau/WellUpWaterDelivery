using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WellUp.Core.Data;
using WellUp.Core.Database;
using WellUp.CustomerPortal.Models.ViewModels;

namespace WellUp.CustomerPortal.Controllers
{
    [Authorize(Policy = "CustomerOnly")]
    public class FeedbackController : Controller
    {
        private readonly WellUpDbContext _dbContext;

        public FeedbackController(WellUpDbContext dbContext)
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

            // Get customer's orders with associated feedback
            var orders = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.CustomerFeedbacks)
                .Where(o => o.CustomerId == customerId && o.OrderStatus == "completed")
                .OrderByDescending(o => o.CompletedAt)
                .ToListAsync();

            // Create view model
            var viewModel = new FeedbackViewModel
            {
                Orders = orders,
                ExistingFeedbacks = await _dbContext.CustomerFeedbacks
                    .Where(f => f.CustomerId == customerId)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int orderId)
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the order
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.CustomerId == customerId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction("Index");
            }

            // Check if feedback already exists
            var existingFeedback = await _dbContext.CustomerFeedbacks
                .FirstOrDefaultAsync(f => f.OrderId == orderId && f.CustomerId == customerId);

            if (existingFeedback != null)
            {
                TempData["ErrorMessage"] = "You have already provided feedback for this order.";
                return RedirectToAction("Index");
            }

            // Create view model
            var viewModel = new CreateFeedbackViewModel
            {
                OrderId = orderId,
                OrderItems = order.OrderItems.ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateFeedbackViewModel model)
        {

            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Map the feedback type to match the database constraint
            string dbFeedbackType;
            switch (model.FeedbackType.ToLower())
            {
                case "general":
                    dbFeedbackType = "GENERAL"; // Use the exact value expected by your DB
                    break;
                case "complaint":
                    dbFeedbackType = "COMPLAINT"; // Use the exact value expected by your DB
                    break;
                default:
                    ModelState.AddModelError("FeedbackType", "Invalid feedback type selected.");
                    return View(model);
            }

            // Verify the order belongs to the customer
            var order = await _dbContext.Orders
                .FirstOrDefaultAsync(o => o.OrderId == model.OrderId && o.CustomerId == customerId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction("Index");
            }

            // Check if feedback already exists
            var existingFeedback = await _dbContext.CustomerFeedbacks
                .FirstOrDefaultAsync(f => f.OrderId == model.OrderId && f.CustomerId == customerId);

            if (existingFeedback != null)
            {
                TempData["ErrorMessage"] = "You have already provided feedback for this order.";
                return RedirectToAction("Index");
            }

            // Create new feedback
            var feedback = new CustomerFeedback
            {
                CustomerId = customerId,
                OrderId = model.OrderId,
                FeedbackType = model.FeedbackType,
                Description = model.Description,
                CreatedAt = DateTime.Now
            };

            _dbContext.CustomerFeedbacks.Add(feedback);
            await _dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you for your feedback!";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Details(int id)
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the feedback
            var feedback = await _dbContext.CustomerFeedbacks
                .Include(f => f.Order)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(f => f.FeedbackId == id && f.CustomerId == customerId);

            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }
    }
}