using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using WellUp.AdminPortal.Models.ViewModels;
using WellUp.Core.Data;
using WellUp.Core.Database;
using WellUp.Core.Utilities;

namespace WellUp.AdminPortal.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class OrdersController : Controller
    {
        private readonly WellUpDbContext _dbContext;
        private readonly ProductRelationshipManager _relationshipManager;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(WellUpDbContext dbContext, ProductRelationshipManager relationshipManager, ILogger<OrdersController> logger)
        {
            _dbContext = dbContext;
            _relationshipManager = relationshipManager;
            _logger = logger;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string search = null, string status = null, string dateRange = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            ViewData["Search"] = search;
            ViewData["Status"] = status;
            ViewData["DateRange"] = dateRange;

            // Set date range based on selection
            var today = DateTime.Today;

            if (dateRange != null)
            {
                switch (dateRange)
                {
                    case "today":
                        startDate = today;
                        endDate = today.AddDays(1).AddSeconds(-1);
                        break;
                    case "yesterday":
                        startDate = today.AddDays(-1);
                        endDate = today.AddSeconds(-1);
                        break;
                    case "week":
                        startDate = today.AddDays(-(int)today.DayOfWeek);
                        endDate = startDate.Value.AddDays(7).AddSeconds(-1);
                        break;
                    case "month":
                        startDate = new DateTime(today.Year, today.Month, 1);
                        endDate = startDate.Value.AddMonths(1).AddSeconds(-1);
                        break;
                    case "custom":
                        // Use provided start/end dates
                        break;
                    default:
                        startDate = null;
                        endDate = null;
                        break;
                }
            }

            // Set default date range if not provided
            if (!startDate.HasValue && !endDate.HasValue)
            {
                startDate = today;
                endDate = today.AddDays(1).AddSeconds(-1);
            }

            if (startDate.HasValue)
                ViewData["StartDate"] = startDate.Value.ToString("yyyy-MM-dd");

            if (endDate.HasValue)
                ViewData["EndDate"] = endDate.Value.ToString("yyyy-MM-dd");

            // Create base query with joins for all needed data
            var ordersQuery = from o in _dbContext.Orders
                              join c in _dbContext.Customers on o.CustomerId equals c.CustomerId
                              join cd in _dbContext.CustomerDetails on c.CustomerId equals cd.CustomerId
                              join a in _dbContext.Addresses on o.AddressId equals a.AddressId
                              select new { Order = o, Customer = c, CustomerDetail = cd, Address = a };

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                ordersQuery = ordersQuery.Where(x =>
                    x.CustomerDetail.CustomerName.ToLower().Contains(search) ||
                    x.Order.OrderId.ToString().Contains(search));
            }

            // Apply status filter if provided
            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(x => x.Order.OrderStatus == status);
            }

            // Apply date filter if provided
            if (startDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(x => x.Order.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(x => x.Order.CreatedAt <= endDate.Value);
            }

            // Execute query and fetch results with related delivery information
            var ordersWithDeliveries = await ordersQuery
                .Select(x => new OrderListViewModel
                {
                    OrderId = x.Order.OrderId,
                    CustomerId = x.Customer.CustomerId,
                    CustomerName = x.CustomerDetail.CustomerName,
                    CustomerEmail = x.Customer.CustomerEmail,
                    CustomerPhone = x.CustomerDetail.Phone,
                    DeliveryAddress = $"{x.Address.Street}, {x.Address.Barangay}, {x.Address.CityMunicipality} {x.Address.ZipCode}",
                    OrderDate = (DateTime)x.Order.CreatedAt,
                    PreferredDeliveryTime = x.Order.PreferredDeliveryTime,
                    TotalAmount = x.Order.TotalAmount ?? 0,
                    Status = x.Order.OrderStatus,
                    DeliveryId = x.Order.Delivery.DeliveryId,
                    DeliveryStatus = x.Order.Delivery.Status,
                    ScheduledTime = x.Order.Delivery.ScheduledTime
                })
                .ToListAsync();

            // Calculate dashboard summary counts
            var totalOrders = await _dbContext.Orders.CountAsync();
            var newOrders = await _dbContext.Orders.CountAsync(o => o.CreatedAt >= DateTime.Today && o.OrderStatus == "new");
            var pendingDeliveries = await _dbContext.Deliveries.CountAsync(d => d.Status == "pending" || d.Status == "scheduled");

            ViewBag.TotalOrders = totalOrders;
            ViewBag.NewOrders = newOrders;
            ViewBag.PendingDeliveries = pendingDeliveries;

            // Sort by ScheduledTime (upcoming first) for active orders
            var activeOrders = ordersWithDeliveries
                .Where(o => o.Status == "new" || o.Status == "in_progress")
                .OrderBy(o => o.ScheduledTime.HasValue ? 0 : 1) // Orders with scheduled time first
                .ThenBy(o => o.ScheduledTime) // Then by scheduled time
                .ThenByDescending(o => o.OrderDate) // Then by order date (newest first) if no scheduled time
                .ToList();

            // Get completed orders sorted by order date (newest first)
            var completedOrders = ordersWithDeliveries
                .Where(o => o.Status == "completed" || o.Status == "cancelled")
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            var model = new OrdersViewModel
            {
                ActiveOrders = activeOrders,
                CompletedOrders = completedOrders
            };

            // Get top products from completed orders
            model.TopProducts = await GetTopProductsFromCompletedOrdersAsync(
                startDate.Value,
                endDate.Value
            );

            return View(model);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _dbContext.Orders
                .Include(o => o.Customer)
                .Include(o => o.Address)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            var customerDetails = await _dbContext.CustomerDetails
                .FirstOrDefaultAsync(cd => cd.CustomerId == order.CustomerId);

            var delivery = await _dbContext.Deliveries
                .FirstOrDefaultAsync(d => d.OrderId == order.OrderId);

            var viewModel = new OrderDetailViewModel
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                CustomerName = customerDetails?.CustomerName,
                CustomerEmail = order.Customer.CustomerEmail,
                CustomerPhone = customerDetails?.Phone,
                DeliveryAddress = $"{order.Address.Street}, {order.Address.Barangay}, {order.Address.CityMunicipality} {order.Address.ZipCode}",
                AddressId = order.AddressId,
                OrderDate = (DateTime)order.CreatedAt,
                TotalAmount = order.TotalAmount ?? 0,
                Status = order.OrderStatus,
                PreferredDeliveryTime = order.PreferredDeliveryTime,
                CompletedAt = order.CompletedAt,
                CancelledAt = order.CancelledAt,
                OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.ProductName,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    SubTotal = oi.SubTotal
                }).ToList()
            };

            if (delivery != null)
            {
                viewModel.Delivery = new DeliveryViewModel
                {
                    DeliveryId = delivery.DeliveryId,
                    Status = delivery.Status,
                    ScheduledTime = delivery.ScheduledTime,
                    Notes = delivery.Notes
                };
            }

            return View(viewModel);
        }

        // POST: Orders/UpdateDeliveryStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDeliveryStatus(int orderId, int? deliveryId, string deliveryStatus, DateTime? scheduledTime, string notes)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            Delivery delivery;

            if (!deliveryId.HasValue)
            {
                // Create new delivery record if it doesn't exist
                delivery = new Delivery
                {
                    OrderId = orderId,
                    Status = deliveryStatus,
                    ScheduledTime = scheduledTime ?? DateTime.Now.AddDays(1),
                    Notes = notes ?? ""
                };
                _dbContext.Deliveries.Add(delivery);
            }
            else
            {
                // Update existing delivery
                delivery = await _dbContext.Deliveries.FindAsync(deliveryId.Value);
                if (delivery == null)
                {
                    return NotFound();
                }

                delivery.Status = deliveryStatus;

                if (scheduledTime.HasValue)
                {
                    delivery.ScheduledTime = scheduledTime.Value;
                }

                if (notes != null)
                {
                    delivery.Notes = notes;
                }
            }

            // Update order status based on delivery status
            switch (deliveryStatus)
            {
                case "pending":
                    order.OrderStatus = "new";
                    break;
                case "out_for_delivery":
                    order.OrderStatus = "in_progress";
                    break;
                case "completed":
                    order.OrderStatus = "completed";
                    order.CompletedAt = DateTime.Now;
                    break;
                case "failed":
                    order.OrderStatus = "cancelled";
                    order.CancelledAt = DateTime.Now;

                    // Return stock for cancelled orders
                    await ReturnOrderItemsToStock(orderId);
                    break;
            }

            await _dbContext.SaveChangesAsync();

            // Redirect based on source
            if (Request.Headers["Referer"].ToString().Contains("/Details/"))
            {
                return RedirectToAction(nameof(Details), new { id = orderId });
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to return stock when order is cancelled
        private async Task ReturnOrderItemsToStock(int orderId)
        {
            try
            {
                var orderItems = await _dbContext.OrderItems
                    .Include(oi => oi.Product)
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync();

                foreach (var item in orderItems)
                {
                    // Only process container products
                    if (!string.IsNullOrEmpty(item.Product.ContainerType))
                    {
                        // Return stock to both container variants (with or without refill)
                        await _relationshipManager.UpdateStockWithRelationshipsAsync(
                            item.ProductId,
                            (item.Product.StockQuantity ?? 0) + item.Quantity,
                            $"Order #{orderId} cancelled - Stock returned"
                        );
                    }
                    else
                    {
                        // Return stock for non-container products
                        var product = item.Product;
                        var currentStock = product.StockQuantity ?? 0;
                        product.StockQuantity = currentStock + item.Quantity;

                        // Log the inventory change
                        _dbContext.InventoryLogs.Add(new InventoryLog
                        {
                            ProductId = item.ProductId,
                            PreviousQuantity = currentStock,
                            NewQuantity = currentStock + item.Quantity,
                            ChangeReason = $"Order #{orderId} cancelled - Stock returned",
                            LoggedAt = DateTime.Now
                        });
                    }
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Stock returned for cancelled order #{orderId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error returning stock for order #{orderId}");
                // Continue with order cancellation even if stock return fails
            }
        }

        // POST: Orders/ScheduleDelivery
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleDelivery(int orderId, DateTime scheduledTime, string notes)
        {
            // Check if delivery record exists
            var existingDelivery = await _dbContext.Deliveries.FirstOrDefaultAsync(d => d.OrderId == orderId);

            if (existingDelivery != null)
            {
                // Update existing delivery
                existingDelivery.Status = "scheduled";
                existingDelivery.ScheduledTime = scheduledTime;
                existingDelivery.Notes = notes ?? existingDelivery.Notes;
            }
            else
            {
                // Create new delivery record
                var delivery = new Delivery
                {
                    OrderId = orderId,
                    Status = "scheduled",
                    ScheduledTime = scheduledTime,
                    Notes = notes ?? ""
                };
                _dbContext.Deliveries.Add(delivery);
            }

            // Update order status
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.OrderStatus = "in_progress";
            }

            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // POST: Orders/ClearOrdersData
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearOrdersData()
        {
            try
            {
                // Get all orders
                var orders = await _dbContext.Orders.ToListAsync();

                // Get all order items and deliveries for those orders
                var orderIds = orders.Select(o => o.OrderId).ToList();
                var orderItems = await _dbContext.OrderItems
                    .Where(oi => orderIds.Contains(oi.OrderId))
                    .ToListAsync();

                var deliveries = await _dbContext.Deliveries
                    .Where(d => orderIds.Contains(d.OrderId))
                    .ToListAsync();

                // Remove all in the correct order to respect foreign keys
                _dbContext.OrderItems.RemoveRange(orderItems);
                _dbContext.Deliveries.RemoveRange(deliveries);
                _dbContext.Orders.RemoveRange(orders);

                await _dbContext.SaveChangesAsync();

                TempData["SuccessMessage"] = "All order data has been successfully cleared.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error clearing data: {ex.Message}";
                _logger.LogError(ex, "Error clearing orders data");
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to get top products from completed orders
        private async Task<List<TopProductViewModel>> GetTopProductsFromCompletedOrdersAsync(DateTime startDate, DateTime endDate, int count = 5)
        {
            // Calculate top products - ONLY from completed orders
            var topProducts = await _dbContext.OrderItems
                .Where(oi => _dbContext.Orders
                    .Where(o => o.CreatedAt >= startDate &&
                              o.CreatedAt <= endDate &&
                              o.OrderStatus == "completed") // Only from completed orders
                    .Select(o => o.OrderId)
                    .Contains(oi.OrderId))
                .Join(_dbContext.Products,
                      oi => oi.ProductId,
                      p => p.ProductId,
                      (oi, p) => new { OrderItem = oi, Product = p })
                .GroupBy(x => new { x.Product.ProductId, x.Product.ProductName })
                .Select(g => new TopProductViewModel
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    Quantity = g.Sum(x => x.OrderItem.Quantity),
                    Revenue = g.Sum(x => x.OrderItem.SubTotal)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(count)
                .ToListAsync();

            return topProducts;
        }
    }
}