//Controllers/CartController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WellUp.Core.Data;
using WellUp.Core.Database;
using WellUp.Core.Utilities;
using WellUp.CustomerPortal.Models.ViewModels;

namespace WellUp.CustomerPortal.Controllers
{
    [Authorize(Policy = "CustomerOnly")]
    public class CartController : Controller
    {
        private readonly WellUpDbContext _dbContext;
        private readonly ILogger<CartController> _logger;
        private readonly ProductRelationshipManager _productRelationshipManager;

        public CartController(WellUpDbContext dbContext, ILogger<CartController> logger,
            ProductRelationshipManager productRelationshipManager)
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

            // Get cart from session
            var cartItems = HttpContext.Session.Get<List<CartItemViewModel>>("CartItems") ?? new List<CartItemViewModel>();

            // Update stock quantity for each item in cart
            foreach (var item in cartItems)
            {
                var product = await _dbContext.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    // Use effective stock quantity
                    int effectiveStock = await _productRelationshipManager.GetEffectiveStockQuantityAsync(item.ProductId);
                    item.StockQuantity = effectiveStock;
                    item.UnitPrice = product.Price; // Ensure unit price is set correctly
                    item.SubTotal = item.UnitPrice * item.Quantity; // Recalculate subtotal
                    item.ContainerType = product.ContainerType; // Add container type

                    // Ensure quantity doesn't exceed stock
                    if (item.Quantity > item.StockQuantity && item.StockQuantity > 0)
                    {
                        item.Quantity = item.StockQuantity;
                        item.SubTotal = item.UnitPrice * item.Quantity; // Update subtotal after quantity change
                    }
                }
            }

            // Save the updated cart back to session
            HttpContext.Session.Set("CartItems", cartItems);

            // Get customer's addresses
            var addresses = await _dbContext.Addresses
                .Where(a => a.CustomerId == customerId)
                .ToListAsync();

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

            // Create cart view model - only set the Items property
            // The other properties (TotalAmount, ItemCount, HasItems) will be computed automatically
            var model = new CartViewModel
            {
                Items = cartItems,
                AvailableAddresses = addresses,
                AddressId = addresses.FirstOrDefault(a => a.IsDefault)?.AddressId ?? addresses.FirstOrDefault()?.AddressId ?? 0,
                PreferredDeliveryTime = defaultDeliveryTime,
                EarliestDeliveryTime = isSameDayAvailable ? today.Add(earliestTime) : tomorrow.Add(earliestTime),
                LatestDeliveryTime = tomorrow.AddDays(6).Add(latestTime), // Allow scheduling up to a week in advance
                IsSameDayAvailable = isSameDayAvailable
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "Quantity must be greater than 0";
                return RedirectToAction("Index", "Products");
            }

            // Get product and check stock
            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found";
                return RedirectToAction("Index", "Products");
            }

            // Use effective stock quantity to account for related products
            int effectiveStock = await _productRelationshipManager.GetEffectiveStockQuantityAsync(productId);
            if (effectiveStock < quantity)
            {
                TempData["ErrorMessage"] = $"Not enough stock available. Only {effectiveStock} units in stock.";
                return RedirectToAction("Index", "Products");
            }

            // Get cart from session
            var cartItems = HttpContext.Session.Get<List<CartItemViewModel>>("CartItems") ?? new List<CartItemViewModel>();

            // Check if product already exists in cart
            var existingItem = cartItems.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                // Update existing item
                int newQuantity = existingItem.Quantity + quantity;

                // Ensure quantity doesn't exceed stock
                if (newQuantity > effectiveStock)
                {
                    newQuantity = effectiveStock;
                    TempData["WarningMessage"] = $"Quantity adjusted to match available stock ({effectiveStock}).";
                }

                existingItem.Quantity = newQuantity;
                existingItem.SubTotal = existingItem.UnitPrice * existingItem.Quantity;
            }
            else
            {
                // Add new item
                cartItems.Add(new CartItemViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    ImagePath = product.ImagePath ?? GetDefaultImagePath(product),
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    SubTotal = product.Price * quantity,
                    StockQuantity = effectiveStock,
                    IsRefillService = product.ContainerType == null && product.IncludesRefill,
                    ContainerType = product.ContainerType
                });
            }

            // Save cart to session
            HttpContext.Session.Set("CartItems", cartItems);

            TempData["SuccessMessage"] = $"{product.ProductName} added to your cart.";
            return RedirectToAction("Index", "Products");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            // Get cart from session
            var cartItems = HttpContext.Session.Get<List<CartItemViewModel>>("CartItems") ?? new List<CartItemViewModel>();

            // Find the item
            var item = cartItems.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                // Store the old quantity for logging/debugging
                var oldQuantity = item.Quantity;

                if (quantity <= 0)
                {
                    // Remove item if quantity is zero or negative
                    cartItems.Remove(item);
                }
                else
                {
                    // Get the product to check stock and price
                    var product = await _dbContext.Products.FindAsync(productId);
                    if (product != null)
                    {
                        // Use effective stock quantity
                        int effectiveStock = await _productRelationshipManager.GetEffectiveStockQuantityAsync(productId);

                        // Ensure quantity doesn't exceed stock
                        if (quantity > effectiveStock)
                        {
                            quantity = effectiveStock;
                            TempData["WarningMessage"] = $"Quantity adjusted to match available stock ({effectiveStock}).";
                        }

                        // Set the quantity and recalculate subtotal
                        item.Quantity = quantity;
                        item.UnitPrice = product.Price; // Ensure price is correct
                        item.SubTotal = item.UnitPrice * item.Quantity; // Recalculate subtotal
                    }
                }

                // Save cart back to session
                HttpContext.Session.Set("CartItems", cartItems);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveItem(int productId)
        {
            // Get cart from session
            var cartItems = HttpContext.Session.Get<List<CartItemViewModel>>("CartItems") ?? new List<CartItemViewModel>();

            // Find the item
            var item = cartItems.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                // Remove item
                cartItems.Remove(item);

                // Save cart back to session
                HttpContext.Session.Set("CartItems", cartItems);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            // Clear the cart session
            HttpContext.Session.Remove("CartItems");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CartViewModel model, Dictionary<int, string> itemNotes)
        {
            try
            {
                // Get current customer ID from claims
                if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Get cart from session
                var cartItems = HttpContext.Session.Get<List<CartItemViewModel>>("CartItems") ?? new List<CartItemViewModel>();

                if (!cartItems.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty.";
                    return RedirectToAction(nameof(Index));
                }

                // Verify the preferred delivery time is valid
                var now = DateTime.Now;
                var earliestTime = new TimeSpan(8, 0, 0); // 8:00 AM
                var latestTime = new TimeSpan(18, 30, 0); // 6:30 PM

                if (model.PreferredDeliveryTime.TimeOfDay < earliestTime ||
                    model.PreferredDeliveryTime.TimeOfDay > latestTime)
                {
                    TempData["ErrorMessage"] = "Delivery time must be between 8:00 AM and 6:30 PM.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate address
                var address = await _dbContext.Addresses
                    .FirstOrDefaultAsync(a => a.AddressId == model.AddressId && a.CustomerId == customerId);

                if (address == null)
                {
                    TempData["ErrorMessage"] = "Invalid delivery address.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate required notes for refill service
                foreach (var item in cartItems)
                {
                    if (item.IsRefillService && string.IsNullOrWhiteSpace(itemNotes[item.ProductId]))
                    {
                        TempData["ErrorMessage"] = "Please provide container details for refill service.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Update notes from form
                    if (itemNotes.ContainsKey(item.ProductId))
                    {
                        item.Notes = itemNotes[item.ProductId];
                    }
                }

                // Calculate total amount
                decimal totalAmount = cartItems.Sum(i => i.SubTotal);

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
                            TotalAmount = totalAmount
                        };

                        _dbContext.Orders.Add(order);
                        await _dbContext.SaveChangesAsync();

                        // Create order items
                        var orderItems = new List<OrderItem>();
                        foreach (var item in cartItems)
                        {
                            var orderItem = new OrderItem
                            {
                                OrderId = order.OrderId,
                                ProductId = item.ProductId,
                                Quantity = item.Quantity,
                                UnitPrice = item.UnitPrice,
                                SubTotal = item.SubTotal
                            };

                            _dbContext.OrderItems.Add(orderItem);
                            orderItems.Add(orderItem);
                        }

                        // Create delivery record
                        var delivery = new Delivery
                        {
                            OrderId = order.OrderId,
                            Status = "pending",
                            ScheduledTime = model.PreferredDeliveryTime,
                            Notes = string.Join(" | ", cartItems.Select(i =>
                                !string.IsNullOrEmpty(i.Notes) ?
                                $"{i.ProductName}: {i.Notes}" :
                                i.ProductName))
                        };

                        _dbContext.Deliveries.Add(delivery);
                        await _dbContext.SaveChangesAsync();

                        // Update stock for all items using ProductRelationshipManager
                        foreach (var item in orderItems)
                        {
                            var product = await _dbContext.Products.FindAsync(item.ProductId);
                            if (product != null)
                            {
                                // Use the ProductRelationshipManager to update stock with relationships
                                int newStockQuantity = (product.StockQuantity ?? 0) - item.Quantity;
                                await _productRelationshipManager.UpdateStockWithRelationshipsAsync(
                                    product.ProductId,
                                    newStockQuantity,
                                    $"Order #{order.OrderId} placed"
                                );
                            }
                        }

                        await transaction.CommitAsync();

                        // Clear the cart after successful order
                        HttpContext.Session.Remove("CartItems");
                        TempData["OrderId"] = order.OrderId;
                        return RedirectToAction("Success", "Orders");
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error placing order: {Message}", ex.Message);

                        var innerException = ex.InnerException != null ? ex.InnerException.Message : "No inner exception";
                        var stackTrace = ex.StackTrace;

                        // Store detailed error information in TempData
                        TempData["ErrorMessage"] = $"An error occurred while placing your order: {ex.Message}";
                        TempData["DetailedError"] = $"Inner Exception: {innerException}\nStack Trace: {stackTrace}";

                        return RedirectToAction(nameof(Index));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PlaceOrder: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
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