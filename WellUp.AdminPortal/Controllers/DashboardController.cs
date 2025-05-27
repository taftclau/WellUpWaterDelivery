// DashboardController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WellUp.AdminPortal.Models.ViewModels;
using WellUp.Core.Data;

namespace WellUp.AdminPortal.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class DashboardController : Controller
    {
        private readonly WellUpDbContext _dbContext;

        public DashboardController(WellUpDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            // Create dashboard view model
            var dashboardViewModel = new DashboardViewModel();

            // Get today's date
            var today = DateTime.Today;

            // Fetch summary statistics
            dashboardViewModel.TotalOrders = await _dbContext.Orders.CountAsync();
            dashboardViewModel.NewOrders = await _dbContext.Orders.CountAsync(o => o.OrderStatus == "new");
            dashboardViewModel.PendingDeliveries = await _dbContext.Deliveries.CountAsync(d =>
                d.Status == "pending" || d.Status == "scheduled" || d.Status == "out_for_delivery");
            dashboardViewModel.LowStockProducts = await _dbContext.Products.CountAsync(p => p.StockQuantity <= p.LowStockThreshold);
            dashboardViewModel.TotalRevenue = await _dbContext.Orders
                .Where(o => o.OrderStatus != "cancelled")
                .SumAsync(o => o.TotalAmount ?? 0);
            dashboardViewModel.TotalCustomers = await _dbContext.Customers.CountAsync();

            // Fetch recent orders using query syntax to avoid conversion issues
            var recentOrders = await (from o in _dbContext.Orders
                                      join c in _dbContext.Customers on o.CustomerId equals c.CustomerId
                                      join cd in _dbContext.CustomerDetails on c.CustomerId equals cd.CustomerId
                                      orderby o.CreatedAt descending
                                      select new OrderViewModel
                                      {
                                          OrderId = o.OrderId,
                                          CustomerName = cd.CustomerName,
                                          OrderDate = (DateTime)o.CreatedAt,
                                          TotalAmount = o.TotalAmount ?? 0,
                                          Status = o.OrderStatus
                                      }).Take(5).ToListAsync();

            dashboardViewModel.RecentOrders = recentOrders;

            // Fetch low stock items
            dashboardViewModel.LowStockItems = await _dbContext.Products
                .Where(p => p.StockQuantity <= p.LowStockThreshold)
                .OrderBy(p => p.StockQuantity)
                .Take(5)
                .Select(p => new DashboardProductViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    StockQuantity = (int)p.StockQuantity,
                    LowStockThreshold = (int)p.LowStockThreshold,
                    Price = p.Price,
                    IsAvailable = (bool)p.IsAvailable
                })
                .ToListAsync();

            // Gather recent activities from multiple sources
            var activities = new List<ActivityViewModel>();

            // Get recent orders for activities using query syntax
            var orderActivities = await (from o in _dbContext.Orders
                                         join c in _dbContext.Customers on o.CustomerId equals c.CustomerId
                                         join cd in _dbContext.CustomerDetails on c.CustomerId equals cd.CustomerId
                                         orderby o.CreatedAt descending
                                         select new ActivityViewModel
                                         {
                                             Type = "order",
                                             Description = $"New order #{o.OrderId:D5} from {cd.CustomerName}",
                                             Timestamp = (DateTime)o.CreatedAt,
                                             Icon = "shopping-cart",
                                             IconClass = "bg-primary-soft"
                                         }).Take(3).ToListAsync();

            activities.AddRange(orderActivities);

            // Get recent delivery status changes using query syntax and proper DateTime handling
            var currentTime = DateTime.Now;
            var deliveryActivities = await (from d in _dbContext.Deliveries
                                            join o in _dbContext.Orders on d.OrderId equals o.OrderId
                                            where d.Status == "completed" || d.Status == "out_for_delivery"
                                            orderby d.ScheduledTime != null ? d.ScheduledTime : currentTime descending
                                            select new ActivityViewModel
                                            {
                                                Type = "delivery",
                                                Description = d.Status == "completed"
                                                    ? $"Order #{o.OrderId:D5} has been delivered"
                                                    : $"Order #{o.OrderId:D5} is out for delivery",
                                                Timestamp = d.ScheduledTime != null ? (DateTime)d.ScheduledTime : currentTime,
                                                Icon = d.Status == "completed" ? "check-circle" : "truck",
                                                IconClass = d.Status == "completed" ? "bg-success-soft" : "bg-info-soft"
                                            }).Take(2).ToListAsync();

            activities.AddRange(deliveryActivities);

            // Get recent inventory logs using query syntax
            var inventoryActivities = await (from l in _dbContext.InventoryLogs
                                             join p in _dbContext.Products on l.ProductId equals p.ProductId
                                             orderby l.LoggedAt descending
                                             select new ActivityViewModel
                                             {
                                                 Type = "inventory",
                                                 Description = $"Inventory updated for {p.ProductName}: {l.PreviousQuantity} → {l.NewQuantity}",
                                                 Timestamp = (DateTime)l.LoggedAt,
                                                 Icon = "boxes",
                                                 IconClass = "bg-warning-soft"
                                             }).Take(2).ToListAsync();

            activities.AddRange(inventoryActivities);

            // Get recent customer registrations using query syntax
            var customerActivities = await (from c in _dbContext.Customers
                                            join cd in _dbContext.CustomerDetails on c.CustomerId equals cd.CustomerId
                                            orderby c.DateCreated descending
                                            select new ActivityViewModel
                                            {
                                                Type = "customer",
                                                Description = $"New customer registered: {cd.CustomerName}",
                                                Timestamp = (DateTime)c.DateCreated,
                                                Icon = "user-plus",
                                                IconClass = "bg-accent-soft"
                                            }).Take(2).ToListAsync();

            activities.AddRange(customerActivities);

            // Sort all activities by timestamp and take most recent
            dashboardViewModel.RecentActivities = activities
                .OrderByDescending(a => a.Timestamp)
                .Take(5)
                .ToList();

            return View(dashboardViewModel);
        }
    }
}