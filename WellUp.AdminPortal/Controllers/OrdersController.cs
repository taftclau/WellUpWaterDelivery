// OrdersController.cs


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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


        public OrdersController(WellUpDbContext dbContext, ProductRelationshipManager relationshipManager)
        {
            _dbContext = dbContext;
            _relationshipManager = relationshipManager;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string filter = null)
        {
            // Create base query using query syntax to avoid conversion issues
            var query = from o in _dbContext.Orders
                        join c in _dbContext.Customers on o.CustomerId equals c.CustomerId
                        join cd in _dbContext.CustomerDetails on c.CustomerId equals cd.CustomerId
                        join a in _dbContext.Addresses on o.AddressId equals a.AddressId
                        select new { Order = o, Customer = c, CustomerDetail = cd, Address = a };

            // Apply filter if specified
            if (!string.IsNullOrEmpty(filter))
            {
                switch (filter.ToLower())
                {
                    case "new":
                        query = query.Where(j => j.Order.OrderStatus == "new");
                        ViewData["FilterName"] = "New Orders";
                        break;
                    case "in_progress":
                        query = query.Where(j => j.Order.OrderStatus == "in_progress");
                        ViewData["FilterName"] = "In-Progress Orders";
                        break;
                    case "completed":
                        query = query.Where(j => j.Order.OrderStatus == "completed");
                        ViewData["FilterName"] = "Completed Orders";
                        break;
                    case "cancelled":
                        query = query.Where(j => j.Order.OrderStatus == "cancelled");
                        ViewData["FilterName"] = "Cancelled Orders";
                        break;
                    case "today":
                        var today = DateTime.Today;
                        query = query.Where(j => ((DateTime)j.Order.CreatedAt).Date == today);
                        ViewData["FilterName"] = "Today's Orders";
                        break;
                }
            }
            else
            {
                ViewData["FilterName"] = "All Orders";
            }

            // Execute query and map to view model
            var orders = await query
                .OrderByDescending(j => j.Order.CreatedAt)
                .Select(j => new OrderListViewModel
                {
                    OrderId = j.Order.OrderId,
                    CustomerName = j.CustomerDetail.CustomerName,
                    CustomerEmail = j.Customer.CustomerEmail,
                    CustomerPhone = j.CustomerDetail.Phone,
                    OrderDate = (DateTime)j.Order.CreatedAt,
                    PreferredDeliveryTime = j.Order.PreferredDeliveryTime,
                    TotalAmount = j.Order.TotalAmount ?? 0,
                    Status = j.Order.OrderStatus,
                    DeliveryAddress = $"{j.Address.Street}, {j.Address.Barangay}, {j.Address.CityMunicipality}"
                })
                .ToListAsync();

            return View(orders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int id)
        {
            // Get order details with customer info using query syntax
            var orderDetailQuery = from o in _dbContext.Orders
                                   where o.OrderId == id
                                   join c in _dbContext.Customers on o.CustomerId equals c.CustomerId
                                   join cd in _dbContext.CustomerDetails on c.CustomerId equals cd.CustomerId
                                   join a in _dbContext.Addresses on o.AddressId equals a.AddressId
                                   select new OrderDetailViewModel
                                   {
                                       OrderId = o.OrderId,
                                       CustomerName = cd.CustomerName,
                                       CustomerEmail = c.CustomerEmail,
                                       CustomerPhone = cd.Phone,
                                       OrderDate = (DateTime)o.CreatedAt,
                                       PreferredDeliveryTime = o.PreferredDeliveryTime,
                                       TotalAmount = o.TotalAmount ?? 0,
                                       Status = o.OrderStatus,
                                       CompletedAt = o.CompletedAt,
                                       CancelledAt = o.CancelledAt,
                                       DeliveryAddress = $"{a.Street}, {a.Barangay}, {a.CityMunicipality}, {a.ZipCode}",
                                       CustomerId = c.CustomerId,
                                       AddressId = a.AddressId
                                   };

            var orderDetail = await orderDetailQuery.FirstOrDefaultAsync();

            if (orderDetail == null)
            {
                return NotFound();
            }

            // Get order items using query syntax
            var orderItems = await (from oi in _dbContext.OrderItems
                                    where oi.OrderId == id
                                    join p in _dbContext.Products on oi.ProductId equals p.ProductId
                                    select new OrderItemViewModel
                                    {
                                        ProductId = p.ProductId,
                                        ProductName = p.ProductName,
                                        Quantity = oi.Quantity,
                                        UnitPrice = oi.UnitPrice,
                                        SubTotal = oi.SubTotal
                                    }).ToListAsync();

            orderDetail.OrderItems = orderItems;

            // Get delivery information if exists
            var delivery = await _dbContext.Deliveries
                .Where(d => d.OrderId == id)
                .Select(d => new DeliveryViewModel
                {
                    DeliveryId = d.DeliveryId,
                    Status = d.Status,
                    ScheduledTime = d.ScheduledTime,
                    Notes = d.Notes
                })
                .FirstOrDefaultAsync();

            if (delivery != null)
            {
                orderDetail.Delivery = delivery;
            }

            return View(orderDetail);
        }

        // GET: Orders/Deliveries
        public async Task<IActionResult> Deliveries(string status = null)
        {
            // Create base query using query syntax
            var query = from d in _dbContext.Deliveries
                        join o in _dbContext.Orders on d.OrderId equals o.OrderId
                        join c in _dbContext.Customers on o.CustomerId equals c.CustomerId
                        join cd in _dbContext.CustomerDetails on c.CustomerId equals cd.CustomerId
                        join a in _dbContext.Addresses on o.AddressId equals a.AddressId
                        select new { Delivery = d, Order = o, Customer = c, CustomerDetail = cd, Address = a };

            // Apply filter if specified
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "scheduled":
                        query = query.Where(j => j.Delivery.Status == "scheduled");
                        ViewData["FilterName"] = "Scheduled Deliveries";
                        break;
                    case "out_for_delivery":
                        query = query.Where(j => j.Delivery.Status == "out_for_delivery");
                        ViewData["FilterName"] = "Out For Delivery";
                        break;
                    case "completed":
                        query = query.Where(j => j.Delivery.Status == "completed");
                        ViewData["FilterName"] = "Completed Deliveries";
                        break;
                    case "failed":
                        query = query.Where(j => j.Delivery.Status == "failed");
                        ViewData["FilterName"] = "Failed Deliveries";
                        break;
                    case "today":
                        var today = DateTime.Today;
                        query = query.Where(j => j.Delivery.ScheduledTime != null &&
                                                ((DateTime)j.Delivery.ScheduledTime).Date == today);
                        ViewData["FilterName"] = "Today's Deliveries";
                        break;
                    default:
                        query = query.Where(j => j.Delivery.Status != "completed" && j.Delivery.Status != "failed");
                        ViewData["FilterName"] = "Pending Deliveries";
                        break;
                }
            }
            else
            {
                // Default to showing only active deliveries
                query = query.Where(j => j.Delivery.Status != "completed" && j.Delivery.Status != "failed");
                ViewData["FilterName"] = "Pending Deliveries";
            }

            // Execute query and map to view model
            var currentTime = DateTime.Now;
            var deliveries = await query
                .OrderBy(j => j.Delivery.ScheduledTime)
                .Select(j => new DeliveryListViewModel
                {
                    DeliveryId = j.Delivery.DeliveryId,
                    OrderId = j.Order.OrderId,
                    CustomerName = j.CustomerDetail.CustomerName,
                    CustomerPhone = j.CustomerDetail.Phone,
                    ScheduledTime = j.Delivery.ScheduledTime,
                    Status = j.Delivery.Status,
                    Notes = j.Delivery.Notes,
                    DeliveryAddress = $"{j.Address.Street}, {j.Address.Barangay}, {j.Address.CityMunicipality}",
                    TotalAmount = j.Order.TotalAmount ?? 0,
                    OrderDate = (DateTime)j.Order.CreatedAt
                })
                .ToListAsync();

            return View(deliveries);
        }

        // POST: Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Update order status
            order.OrderStatus = status;
            order.UpdatedAt = DateTime.Now;

            // Set CompletedAt or CancelledAt based on status
            if (status == "completed" && !order.CompletedAt.HasValue)
            {
                order.CompletedAt = DateTime.Now;

                // If completed, reduce stock for all products in the order
                var orderItems = await _dbContext.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync();

                foreach (var item in orderItems)
                {
                    await UpdateProductStockForOrder(
                        item.ProductId,
                        item.Quantity,
                        orderId
                    );
                }
            }
            else if (status == "cancelled" && !order.CancelledAt.HasValue)
            {
                order.CancelledAt = DateTime.Now;
            }

            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // POST: Orders/ScheduleDelivery
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleDelivery(int orderId, DateTime? scheduledTime, string notes)
        {
            // Check if order exists
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            // Check if delivery already exists
            var existingDelivery = await _dbContext.Deliveries.FirstOrDefaultAsync(d => d.OrderId == orderId);
            if (existingDelivery != null)
            {
                return RedirectToAction(nameof(UpdateDelivery), new { deliveryId = existingDelivery.DeliveryId, scheduledTime, notes });
            }

            // Create new delivery
            var delivery = new Delivery
            {
                OrderId = orderId,
                Status = "scheduled",
                ScheduledTime = (DateTime)scheduledTime,
                Notes = notes
            };

            _dbContext.Deliveries.Add(delivery);

            // Update order status if it's still 'new'
            if (order.OrderStatus == "new")
            {
                order.OrderStatus = "in_progress";
                order.UpdatedAt = DateTime.Now;
            }

            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // POST: Orders/UpdateDelivery
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDelivery(int deliveryId, string status, DateTime? scheduledTime, string notes)
        {
            var delivery = await _dbContext.Deliveries.FindAsync(deliveryId);

            if (delivery == null)
            {
                return NotFound();
            }

            // Update delivery information
            delivery.Status = status;
            delivery.ScheduledTime = (DateTime)scheduledTime;
            delivery.Notes = notes;

            // If delivery is completed, update order status too
            if (status == "completed")
            {
                var order = await _dbContext.Orders.FindAsync(delivery.OrderId);
                if (order != null && order.OrderStatus != "completed")
                {
                    order.OrderStatus = "completed";
                    order.CompletedAt = DateTime.Now;
                    order.UpdatedAt = DateTime.Now;
                }
            }

            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = delivery.OrderId });
        }

        // POST: Orders/CancelOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _dbContext.Orders.FindAsync(orderId);

            if (order == null)
            {
                return NotFound();
            }

            // Update order status
            order.OrderStatus = "cancelled";
            order.CancelledAt = DateTime.Now;
            order.UpdatedAt = DateTime.Now;

            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = orderId });
        }
        private async Task UpdateProductStockForOrder(int productId, int quantityToDecrease, int orderId)
        {
            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null) return;

            // Get current effective stock
            int currentStock = await _relationshipManager.GetEffectiveStockQuantityAsync(productId);
            int newStock = Math.Max(0, currentStock - quantityToDecrease);

            // Update stock with relationship handling
            await _relationshipManager.UpdateStockWithRelationshipsAsync(
                productId,
                newStock,
                $"Order deduction - Order #{orderId}"
            );
        }
    }
}