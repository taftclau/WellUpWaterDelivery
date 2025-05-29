//CustomersController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using WellUp.AdminPortal.Models.ViewModels;
using WellUp.Core.Data;

namespace WellUp.AdminPortal.Controllers
{
    public class CustomersController : Controller
    {
        private readonly WellUpDbContext _context;

        public CustomersController(WellUpDbContext context)
        {
            _context = context;
        }

        // GET: Customers - Main customer list page
        public async Task<IActionResult> Index(string searchString, string filter, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentFilterType"] = filter;

            // Set up sorting parameters
            ViewData["NameSortParam"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["EmailSortParam"] = sortOrder == "email" ? "email_desc" : "email";
            ViewData["DateSortParam"] = sortOrder == "date" ? "date_desc" : "date";

            // Query customers with their details - Fixed LINQ query with explicit DateTime handling
            var customersQuery = from c in _context.Customers
                                 join cd in _context.CustomerDetails on c.CustomerId equals cd.CustomerId
                                 select new CustomerListItemViewModel
                                 {
                                     CustomerId = c.CustomerId,
                                     CustomerName = cd.CustomerName,
                                     Email = c.CustomerEmail,
                                     Phone = cd.Phone,
                                     DateCreated = c.DateCreated,  // Assuming DateCreated is now DateTime? in the view model
                                     OrderCount = _context.Orders.Count(o => o.CustomerId == c.CustomerId)
                                 };

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                customersQuery = customersQuery.Where(c =>
                    c.CustomerName.ToLower().Contains(searchString) ||
                    c.Email.ToLower().Contains(searchString) ||
                    c.Phone.Contains(searchString)
                );
            }

            // Apply additional filters with proper DateTime handling
            switch (filter)
            {
                case "new_customers":
                    DateTime thirtyDaysAgo = DateTime.Now.AddDays(-30);
                    customersQuery = customersQuery.Where(c => c.DateCreated.HasValue && c.DateCreated.Value >= thirtyDaysAgo);
                    break;
                case "regular_customers":
                    customersQuery = customersQuery.Where(c => c.OrderCount >= 3);
                    break;
                case "inactive_customers":
                    DateTime threeMonthsAgo = DateTime.Now.AddMonths(-3);
                    customersQuery = customersQuery.Where(c => !_context.Orders.Any(o =>
                        o.CustomerId == c.CustomerId && o.CreatedAt.HasValue && o.CreatedAt.Value >= threeMonthsAgo));
                    break;
            }

            // Apply sorting with explicit ordering and nullable DateTime handling
            switch (sortOrder)
            {
                case "name_desc":
                    customersQuery = customersQuery.OrderByDescending(c => c.CustomerName);
                    break;
                case "email":
                    customersQuery = customersQuery.OrderBy(c => c.Email);
                    break;
                case "email_desc":
                    customersQuery = customersQuery.OrderByDescending(c => c.Email);
                    break;
                case "date":
                    customersQuery = customersQuery.OrderBy(c => c.DateCreated);
                    break;
                case "date_desc":
                    customersQuery = customersQuery.OrderByDescending(c => c.DateCreated);
                    break;
                default:
                    customersQuery = customersQuery.OrderBy(c => c.CustomerName);
                    break;
            }

            var feedbackCount = await _context.CustomerFeedbacks
                .Where(f => f.FeedbackType == "complaint")
                .CountAsync();

            var viewModel = new CustomersListViewModel
            {
                Customers = await customersQuery.ToListAsync(),
                ComplaintCount = feedbackCount
            };

            return View(viewModel);
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Get customer info with proper DateTime handling
            var customer = await _context.Customers
                .Where(c => c.CustomerId == id)
                .Select(c => new CustomerDetailsViewModel
                {
                    CustomerId = c.CustomerId,
                    Email = c.CustomerEmail,
                    DateCreated = c.DateCreated,  // Now DateTime? in the view model
                })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound();
            }

            // Get customer details
            var details = await _context.CustomerDetails
                .FirstOrDefaultAsync(cd => cd.CustomerId == id);

            if (details != null)
            {
                customer.CustomerName = details.CustomerName;
                customer.Phone = details.Phone;
            }

            // Get addresses
            customer.Addresses = await _context.Addresses
                .Where(a => a.CustomerId == id)
                .Select(a => new AddressViewModel
                {
                    AddressId = a.AddressId,
                    CityMunicipality = a.CityMunicipality,
                    Barangay = a.Barangay,
                    Street = a.Street,
                    ZipCode = a.ZipCode,
                    IsDefault = a.IsDefault
                })
                .ToListAsync();

            // Get order history with proper DateTime handling
            customer.Orders = await _context.Orders
                .Where(o => o.CustomerId == id)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrderHistoryViewModel
                {
                    OrderId = o.OrderId,
                    OrderDate = o.CreatedAt, // Assuming OrderDate is now DateTime? in the view model
                    Status = o.OrderStatus,
                    TotalAmount = o.TotalAmount ?? 0,
                    ItemCount = _context.OrderItems.Count(oi => oi.OrderId == o.OrderId)
                })
                .ToListAsync();

            // Get feedback history
            customer.Feedback = await _context.CustomerFeedbacks
                .Where(f => f.CustomerId == id.Value)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new FeedbackViewModel
                {
                    FeedbackId = f.FeedbackId,
                    OrderId = f.OrderId,
                    FeedbackType = f.FeedbackType,
                    Description = f.Description,
                    CreatedAt = f.CreatedAt  // Assuming CreatedAt is now DateTime? in the view model
                })
                .ToListAsync();

            return View(customer);
        }

        // Other methods with similar DateTime null handling...

        // GET: Customers/FeedbackDetails/5
        public async Task<IActionResult> FeedbackDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.CustomerFeedbacks
                .FirstOrDefaultAsync(f => f.FeedbackId == id);

            if (feedback == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == feedback.CustomerId);

            var customerDetails = await _context.CustomerDetails
                .FirstOrDefaultAsync(cd => cd.CustomerId == feedback.CustomerId);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == feedback.OrderId);

            var orderItems = await _context.OrderItems
                .Join(_context.Products,
                    oi => oi.ProductId,
                    p => p.ProductId,
                    (oi, p) => new { OrderItem = oi, Product = p })
                .Where(x => x.OrderItem.OrderId == feedback.OrderId)
                .ToListAsync();

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == order.AddressId);

            var viewModel = new FeedbackDetailsViewModel
            {
                FeedbackId = feedback.FeedbackId,
                CustomerId = feedback.CustomerId,
                CustomerName = customerDetails?.CustomerName,
                CustomerEmail = customer.CustomerEmail,
                CustomerPhone = customerDetails?.Phone,
                OrderId = feedback.OrderId,
                OrderDate = order.CreatedAt, // Now DateTime? in the view model
                OrderStatus = order.OrderStatus,
                TotalAmount = order.TotalAmount ?? 0,
                FeedbackType = feedback.FeedbackType,
                Description = feedback.Description,
                CreatedAt = feedback.CreatedAt, // Now DateTime? in the view model
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
    }
}