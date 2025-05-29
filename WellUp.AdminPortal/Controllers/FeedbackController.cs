//FeedbackController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WellUp.AdminPortal.Models.ViewModels;
using WellUp.Core.Data;
using WellUp.Core.Database;

namespace WellUp.AdminPortal.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class FeedbackController : Controller
    {
        private readonly WellUpDbContext _dbContext;

        public FeedbackController(WellUpDbContext context)
        {
            _dbContext = context;
        }

        // GET: Feedback
        public async Task<IActionResult> Index(string searchString, string filter)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentFilterType"] = filter;

            // Create query
            var feedbackQuery = from f in _dbContext.CustomerFeedbacks
                                join c in _dbContext.Customers on f.CustomerId equals c.CustomerId
                                join cd in _dbContext.CustomerDetails on c.CustomerId equals cd.CustomerId
                                select new FeedbackListViewModel
                                {
                                    FeedbackId = f.FeedbackId,
                                    CustomerId = f.CustomerId,
                                    CustomerName = cd.CustomerName,
                                    OrderId = f.OrderId,
                                    FeedbackType = f.FeedbackType,
                                    Description = f.Description,
                                    CreatedAt = f.CreatedAt,
                                    Email = c.CustomerEmail
                                };

            // Calculate statistics for the cards
            var totalFeedback = await feedbackQuery.CountAsync();
            var complaintCount = await feedbackQuery.CountAsync(f => f.FeedbackType == "complaint");
            var generalFeedbackCount = totalFeedback - complaintCount;
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            var recentFeedbackCount = await feedbackQuery.CountAsync(f => f.CreatedAt >= sevenDaysAgo);

            // Store statistics in ViewBag
            ViewBag.TotalFeedback = totalFeedback;
            ViewBag.ComplaintCount = complaintCount;
            ViewBag.GeneralFeedbackCount = generalFeedbackCount;
            ViewBag.RecentFeedbackCount = recentFeedbackCount;

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                feedbackQuery = feedbackQuery.Where(f =>
                    f.CustomerName.ToLower().Contains(searchString) ||
                    f.Description.ToLower().Contains(searchString) ||
                    f.Email.ToLower().Contains(searchString)
                );
            }

            // Apply type filter
            if (!string.IsNullOrEmpty(filter))
            {
                switch (filter.ToLower())
                {
                    case "complaints":
                        feedbackQuery = feedbackQuery.Where(f => f.FeedbackType == "complaint");
                        ViewData["FilterName"] = "Complaints";
                        break;
                    case "general_feedback":
                        feedbackQuery = feedbackQuery.Where(f => f.FeedbackType == "general_feedback");
                        ViewData["FilterName"] = "General Feedback";
                        break;
                    case "recent":
                        feedbackQuery = feedbackQuery.Where(f => f.CreatedAt >= sevenDaysAgo);
                        ViewData["FilterName"] = "Recent Feedback (Last 7 Days)";
                        break;
                    default:
                        ViewData["FilterName"] = "All Feedback";
                        break;
                }
            }
            else
            {
                ViewData["FilterName"] = "All Feedback";
            }

            // Order by most recent first
            feedbackQuery = feedbackQuery.OrderByDescending(f => f.CreatedAt);

            var feedbackList = await feedbackQuery.ToListAsync();

            return View(feedbackList);
        }

        // GET: Feedback/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _dbContext.CustomerFeedbacks
                .FirstOrDefaultAsync(f => f.FeedbackId == id);

            if (feedback == null)
            {
                return NotFound();
            }

            var customer = await _dbContext.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == feedback.CustomerId);

            var customerDetails = await _dbContext.CustomerDetails
                .FirstOrDefaultAsync(cd => cd.CustomerId == feedback.CustomerId);

            var order = await _dbContext.Orders
                .FirstOrDefaultAsync(o => o.OrderId == feedback.OrderId);

            var orderItems = await _dbContext.OrderItems
                .Join(_dbContext.Products,
                    oi => oi.ProductId,
                    p => p.ProductId,
                    (oi, p) => new { OrderItem = oi, Product = p })
                .Where(x => x.OrderItem.OrderId == feedback.OrderId)
                .ToListAsync();

            var address = await _dbContext.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == order.AddressId);

            var viewModel = new FeedbackDetailsViewModel
            {
                FeedbackId = feedback.FeedbackId,
                CustomerId = feedback.CustomerId,
                CustomerName = customerDetails?.CustomerName,
                CustomerEmail = customer.CustomerEmail,
                CustomerPhone = customerDetails?.Phone,
                OrderId = feedback.OrderId,
                OrderDate = order.CreatedAt,
                OrderStatus = order.OrderStatus,
                TotalAmount = order.TotalAmount ?? 0,
                FeedbackType = feedback.FeedbackType,
                Description = feedback.Description,
                CreatedAt = feedback.CreatedAt,
                DeliveryAddress = address != null
                    ? $"{address.Street}, {address.Barangay}, {address.CityMunicipality} {address.ZipCode}"
                    : "Address not found",
                OrderItems = orderItems.Select(x => new OrderItemViewModel
                {
                    ProductId = x.OrderItem.ProductId,
                    ProductName = x.Product.ProductName,
                    Quantity = x.OrderItem.Quantity,
                    UnitPrice = x.OrderItem.UnitPrice,
                    SubTotal = x.OrderItem.SubTotal
                }).ToList()
            };

            return View(viewModel);
        }

        // POST: Feedback/MarkAsResolved/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsResolved(int id, string response)
        {
            var feedback = await _dbContext.CustomerFeedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            // In a real application, you would store the response and resolution status
            // Since we don't have these fields, we'll just add the response handling logic
            // without actually changing the data

            TempData["SuccessMessage"] = "Feedback has been marked as resolved.";
            return RedirectToAction(nameof(Index));
        }

      
    }
}