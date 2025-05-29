//Controllers/OrdersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using WellUp.Core.Data;
using WellUp.Core.Database;
using WellUp.Core.Utilities;
using WellUp.CustomerPortal.Models.ViewModels;

namespace WellUp.CustomerPortal.Controllers
{
    [Authorize(Policy = "CustomerOnly")]
    public class OrdersController : Controller
    {
        private readonly WellUpDbContext _dbContext;
        private readonly ILogger<OrdersController> _logger;
        private readonly ProductRelationshipManager _productRelationshipManager;

        public OrdersController(WellUpDbContext dbContext, ILogger<OrdersController> logger, ProductRelationshipManager productRelationshipManager)
        {
            _dbContext = dbContext;
            _logger = logger;
            _productRelationshipManager = productRelationshipManager;
        }

        public async Task<IActionResult> Index()
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get customer's orders
            var orders = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Address)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> History()
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get all customer's orders
            var allOrders = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Delivery)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // Create view model
            var viewModel = new OrderHistoryViewModel
            {
                CurrentOrders = allOrders
                    .Where(o => o.OrderStatus == "new" || o.OrderStatus == "in_progress")
                    .Select(o => new HistorySummaryViewModel
                    {
                        OrderId = o.OrderId,
                        OrderDate = o.CreatedAt ?? DateTime.Now,
                        OrderStatus = o.OrderStatus,
                        TotalAmount = o.TotalAmount ?? 0,
                        ItemCount = o.OrderItems.Count,
                        DeliveryDate = o.Delivery?.ScheduledTime,
                        DeliveryStatus = o.Delivery?.Status,
                        CanReorder = true,
                        CanCancel = o.OrderStatus == "new" // Only allow cancellation of new orders
                    })
                    .ToList(),

                PastOrders = allOrders
                    .Where(o => o.OrderStatus == "completed" || o.OrderStatus == "cancelled")
                    .Select(o => new HistorySummaryViewModel
                    {
                        OrderId = o.OrderId,
                        OrderDate = o.CreatedAt ?? DateTime.Now,
                        OrderStatus = o.OrderStatus,
                        TotalAmount = o.TotalAmount ?? 0,
                        ItemCount = o.OrderItems.Count,
                        DeliveryDate = o.Delivery?.ScheduledTime,
                        DeliveryStatus = o.Delivery?.Status,
                        CanReorder = o.OrderStatus == "completed", // Only completed orders can be reordered
                        CanCancel = false // Past orders cannot be cancelled
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the order with related data
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.Address)
                .Include(o => o.Delivery)
                .Include(o => o.CustomerFeedbacks)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == customerId);

            if (order == null)
            {
                return NotFound();
            }

            // Check if products are still available for reorder
            var orderItems = new List<OrderItemDetailsViewModel>();
            foreach (var item in order.OrderItems)
            {
                var product = await _dbContext.Products.FindAsync(item.ProductId);
                bool isAvailable = product != null && product.IsAvailable == true && product.StockQuantity > 0;

                orderItems.Add(new OrderItemDetailsViewModel
                {
                    OrderItemId = item.OrderItemId,
                    ProductId = item.ProductId,
                    ProductName = item.Product.ProductName,
                    ProductImagePath = item.Product.ImagePath ?? GetDefaultImagePath(item.Product),
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SubTotal = item.SubTotal,
                    IsAvailableForReorder = isAvailable,
                    ContainerType = item.Product.ContainerType,
                    IncludesRefill = item.Product.IncludesRefill
                });
            }

            // Create the view model
            var viewModel = new OrderDetailsViewModel
            {
                OrderId = order.OrderId,
                OrderDate = order.CreatedAt ?? DateTime.Now,
                OrderStatus = order.OrderStatus,
                TotalAmount = order.TotalAmount ?? 0,
                DeliveryAddress = order.Address,
                PreferredDeliveryTime = order.PreferredDeliveryTime,
                CompletedAt = order.CompletedAt,
                DeliveryStatus = order.Delivery?.Status,
                ScheduledDeliveryTime = order.Delivery?.ScheduledTime,
                DeliveryNotes = order.Delivery?.Notes,
                Items = orderItems,
                CanReorder = order.OrderStatus == "completed", // Only allow reordering completed orders
                CanCancel = order.OrderStatus == "new", // Only allow cancellation of new orders
                HasFeedback = order.CustomerFeedbacks.Any()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the order
            var order = await _dbContext.Orders
                .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == customerId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction(nameof(History));
            }

            // Check if order can be cancelled
            if (order.OrderStatus != "new")
            {
                TempData["ErrorMessage"] = "Only new orders can be cancelled.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // Inside your CancelOrder method (after verifying the order but before updating order status)
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Get the order items
                var orderItems = await _dbContext.OrderItems
                    .Where(oi => oi.OrderId == id)
                    .ToListAsync();

                // Restore the stock quantities
                foreach (var item in orderItems)
                {
                    var product = await _dbContext.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        // Use the ProductRelationshipManager to update stock with relationships
                        int newStockQuantity = (product.StockQuantity ?? 0) + item.Quantity;
                        await _productRelationshipManager.UpdateStockWithRelationshipsAsync(
                            product.ProductId,
                            newStockQuantity,
                            $"Order #{id} cancelled"
                        );
                    }
                }

                // Update order status
                order.OrderStatus = "cancelled";
                order.CancelledAt = DateTime.Now;
                order.UpdatedAt = DateTime.Now;

                // Also update the delivery status to 'failed'
                var delivery = await _dbContext.Deliveries.FirstOrDefaultAsync(d => d.OrderId == id);
                if (delivery != null)
                {
                    delivery.Status = "failed";
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Order #" + id + " has been cancelled successfully, and stock has been returned to inventory.";
                return RedirectToAction(nameof(History));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling order");
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reorder(int id)
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the order with items
            var order = await _dbContext.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == customerId);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToAction(nameof(History));
            }

            // Get customer's addresses
            var addresses = await _dbContext.Addresses
                .Where(a => a.CustomerId == customerId)
                .ToListAsync();

            if (!addresses.Any())
            {
                TempData["ErrorMessage"] = "You need to add a delivery address before placing an order.";
                return RedirectToAction("Index", "Account");
            }

            // Prepare the cart items for reordering
            var cartItems = new List<CartItemViewModel>();
            bool allItemsAvailable = true;

            foreach (var item in order.OrderItems)
            {
                var product = await _dbContext.Products.FindAsync(item.ProductId);

                if (product == null || product.IsAvailable != true || product.StockQuantity <= 0)
                {
                    allItemsAvailable = false;
                    TempData["WarningMessage"] = "Some items from your previous order are not available and were not added to your cart.";
                    continue;
                }

                // Ensure quantity doesn't exceed available stock
                int quantity = Math.Min(item.Quantity, product.StockQuantity.Value);

                cartItems.Add(new CartItemViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ImagePath = product.ImagePath ?? GetDefaultImagePath(product),
                    Quantity = quantity,
                    UnitPrice = product.Price, // Use current price
                    SubTotal = product.Price * quantity,
                    StockQuantity = product.StockQuantity.Value,
                    IsRefillService = product.ContainerType == null && product.IncludesRefill,
                    ContainerType = product.ContainerType
                });
            }

            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "None of the items from your previous order are available for reorder.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            // Save the cart to session
            HttpContext.Session.Set("CartItems", cartItems);

            if (allItemsAvailable)
            {
                TempData["SuccessMessage"] = "Your previous order has been added to your cart.";
            }

            // Redirect to cart page
            return RedirectToAction("Index", "Cart");
        }

        public async Task<IActionResult> Summary()
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get BuyNow product from TempData
            var buyNowProduct = TempData["BuyNowProduct"];
            if (buyNowProduct == null)
            {
                TempData["ErrorMessage"] = "Product information not found. Please try again.";
                return RedirectToAction("Index", "Products");
            }

            // Deserialize the product from TempData
            var productJson = JsonSerializer.Serialize(buyNowProduct);
            var productInfo = JsonSerializer.Deserialize<dynamic>(productJson);

            // Get customer's addresses
            var addresses = await _dbContext.Addresses
                .Where(a => a.CustomerId == customerId)
                .ToListAsync();

            if (!addresses.Any())
            {
                TempData["ErrorMessage"] = "You need to add a delivery address before placing an order.";
                return RedirectToAction("Index", "Account"); // Redirect to address management
            }

            // Calculate delivery time constraints
            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            DateTime tomorrow = today.AddDays(1);

            // Set delivery hours (8:00 AM to 6:30 PM)
            TimeSpan earliestTime = new TimeSpan(8, 0, 0); // 8:00 AM
            TimeSpan latestTime = new TimeSpan(18, 30, 0); // 6:30 PM

            // Check if current time is before cutoff (e.g., 5:00 PM)
            bool isSameDayAvailable = now.TimeOfDay < new TimeSpan(17, 0, 0); // Before 5:00 PM

            // Set default delivery time (next available slot)
            DateTime defaultDeliveryTime;

            if (isSameDayAvailable && now.TimeOfDay < earliestTime)
            {
                // Today, starting at 8:00 AM
                defaultDeliveryTime = today.Add(earliestTime);
            }
            else if (isSameDayAvailable && now.TimeOfDay < latestTime)
            {
                // Today, starting at next available hour (rounded up)
                int nextHour = now.Hour + 1;
                defaultDeliveryTime = today.AddHours(nextHour);
            }
            else
            {
                // Tomorrow, starting at 8:00 AM
                defaultDeliveryTime = tomorrow.Add(earliestTime);
            }

            // Create the view model
            var model = new OrderSummaryViewModel
            {
                ProductId = productInfo.GetProperty("ProductId").GetInt32(),
                ProductName = productInfo.GetProperty("ProductName").GetString(),
                ProductDescription = productInfo.GetProperty("Description").GetString(),
                ImagePath = productInfo.GetProperty("ImagePath").GetString(),
                UnitPrice = productInfo.GetProperty("UnitPrice").GetDecimal(),
                Quantity = productInfo.GetProperty("Quantity").GetInt32(),
                Subtotal = productInfo.GetProperty("UnitPrice").GetDecimal() * productInfo.GetProperty("Quantity").GetInt32(),
                Notes = productInfo.GetProperty("Notes").GetString(),
                IsRefillService = productInfo.GetProperty("IsRefillService").GetBoolean(),
                AvailableAddresses = addresses,
                AddressId = addresses.FirstOrDefault(a => a.IsDefault)?.AddressId ?? addresses.First().AddressId,
                PreferredDeliveryTime = defaultDeliveryTime,
                EarliestDeliveryTime = isSameDayAvailable ? today.Add(earliestTime) : tomorrow.Add(earliestTime),
                LatestDeliveryTime = tomorrow.AddDays(6).Add(latestTime), // Allow scheduling up to a week in advance
                IsSameDayAvailable = isSameDayAvailable
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(PlaceOrderViewModel model)
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors in the form.";
                return RedirectToAction("Summary");
            }

            // Get the product
            var product = await _dbContext.Products.FindAsync(model.ProductId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                return RedirectToAction("Index", "Products");
            }

            // Check stock availability
            if (product.StockQuantity < model.Quantity)
            {
                TempData["ErrorMessage"] = "Not enough stock available.";
                return RedirectToAction("Index", "Products");
            }

            // Verify the address belongs to the customer
            var address = await _dbContext.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == model.AddressId && a.CustomerId == customerId);

            if (address == null)
            {
                TempData["ErrorMessage"] = "Invalid delivery address.";
                return RedirectToAction("Summary");
            }

            // Validate delivery time
            DateTime now = DateTime.Now;
            TimeSpan earliestTime = new TimeSpan(8, 0, 0); // 8:00 AM
            TimeSpan latestTime = new TimeSpan(18, 30, 0); // 6:30 PM

            if (model.PreferredDeliveryTime.TimeOfDay < earliestTime ||
                model.PreferredDeliveryTime.TimeOfDay > latestTime)
            {
                TempData["ErrorMessage"] = "Delivery time must be between 8:00 AM and 6:30 PM.";
                return RedirectToAction("Summary");
            }

            if (model.PreferredDeliveryTime.Date == now.Date &&
                now.TimeOfDay > new TimeSpan(17, 0, 0)) // After 5:00 PM
            {
                TempData["ErrorMessage"] = "Same-day delivery is no longer available. Please select a future date.";
                return RedirectToAction("Summary");
            }

            // Calculate subtotal
            decimal subtotal = product.Price * model.Quantity;

            // Create the order
            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    // Create order
                    var order = new Order
                    {
                        CustomerId = customerId,
                        AddressId = model.AddressId,
                        OrderStatus = "new",
                        PreferredDeliveryTime = model.PreferredDeliveryTime,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        TotalAmount = subtotal
                    };

                    _dbContext.Orders.Add(order);
                    await _dbContext.SaveChangesAsync();

                    // Create order item
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = model.ProductId,
                        Quantity = model.Quantity,
                        UnitPrice = product.Price,
                        SubTotal = subtotal
                    };

                    _dbContext.OrderItems.Add(orderItem);

                    // Create delivery record
                    var delivery = new Delivery
                    {
                        OrderId = order.OrderId,
                        Status = "scheduled",
                        ScheduledTime = model.PreferredDeliveryTime,
                        Notes = model.Notes ?? ""
                    };

                    _dbContext.Deliveries.Add(delivery);
                    await _dbContext.SaveChangesAsync();

                    // Create a list of order items to process
                    var orderItems = new List<OrderItem> { orderItem };

                    // Adjust the stock quantities for all products
                    foreach (var item in orderItems)
                    {
                        var productToUpdate = await _dbContext.Products.FindAsync(item.ProductId);
                        if (productToUpdate != null)
                        {
                            // Use the ProductRelationshipManager to update stock with relationships
                            int newStockQuantity = (productToUpdate.StockQuantity ?? 0) - item.Quantity;
                            await _productRelationshipManager.UpdateStockWithRelationshipsAsync(
                                productToUpdate.ProductId,
                                newStockQuantity,
                                $"Order #{order.OrderId} placed"
                            );
                        }
                    }

                    await transaction.CommitAsync();

                    // Redirect to success page with order ID
                    TempData["OrderId"] = order.OrderId;
                    return RedirectToAction("Success");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error processing order");
                    throw;
                }
            }
        }

        public IActionResult Success()
        {
            // Get order ID from TempData
            if (TempData["OrderId"] == null)
            {
                return RedirectToAction("Index");
            }

            int orderId = (int)TempData["OrderId"];
            return View(orderId);
        }

        // Helper method to get default image path based on product type
        private string GetDefaultImagePath(Product product)
        {
            if (product == null) return "/images/products/default.png";

            if (product.ContainerType == "slim")
                return "/images/products/slim_container.png";

            if (product.ContainerType == "round")
                return "/images/products/round_container.png";

            if (product.IncludesRefill && product.ContainerType == null)
                return "/images/products/refill.png";

            return "/images/products/default.png";
        }
    }
}