//StatisticsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using WellUp.AdminPortal.Models.ViewModels;
using WellUp.Core.Data;

namespace WellUp.AdminPortal.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class StatisticsController : Controller
    {
        private readonly WellUpDbContext _dbContext;

        public StatisticsController(WellUpDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null, string period = null, string search = null, string sortBy = null)
        {
            // Set default date range if not provided
            var today = DateTime.Today;

            // Handle period selection
            if (period != null)
            {
                switch (period)
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
                    case "year":
                        startDate = new DateTime(today.Year, 1, 1);
                        endDate = startDate.Value.AddYears(1).AddSeconds(-1);
                        break;
                }
            }

            // Default to current month if no dates provided
            if (!startDate.HasValue)
            {
                startDate = new DateTime(today.Year, today.Month, 1);
                endDate = startDate.Value.AddMonths(1).AddSeconds(-1);
                period = "month"; // Default to month view
            }
            else if (!endDate.HasValue)
            {
                endDate = startDate.Value.AddDays(1).AddSeconds(-1);
            }

            // Store the date range and period in ViewData
            ViewData["StartDate"] = startDate.Value.ToString("yyyy-MM-dd");
            ViewData["EndDate"] = endDate.Value.ToString("yyyy-MM-dd");
            ViewData["Period"] = period ?? "custom";
            ViewData["Search"] = search;
            ViewData["SortBy"] = sortBy ?? "date_desc";

            // Create base query using query syntax
            var ordersQuery = from o in _dbContext.Orders
                              join c in _dbContext.Customers on o.CustomerId equals c.CustomerId
                              join cd in _dbContext.CustomerDetails on c.CustomerId equals cd.CustomerId
                              where o.CreatedAt >= startDate && o.CreatedAt <= endDate
                              select new { Order = o, Customer = c, CustomerDetail = cd };

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(search))
            {
                string searchLower = search.ToLower();
                ordersQuery = ordersQuery.Where(x =>
                    x.CustomerDetail.CustomerName.ToLower().Contains(searchLower) ||
                    x.Customer.CustomerEmail.ToLower().Contains(searchLower));
            }

            // Apply sorting
            switch (sortBy)
            {
                case "customer_asc":
                    ordersQuery = ordersQuery.OrderBy(x => x.CustomerDetail.CustomerName);
                    break;
                case "customer_desc":
                    ordersQuery = ordersQuery.OrderByDescending(x => x.CustomerDetail.CustomerName);
                    break;
                case "amount_asc":
                    ordersQuery = ordersQuery.OrderBy(x => x.Order.TotalAmount);
                    break;
                case "amount_desc":
                    ordersQuery = ordersQuery.OrderByDescending(x => x.Order.TotalAmount);
                    break;
                case "date_asc":
                    ordersQuery = ordersQuery.OrderBy(x => x.Order.CreatedAt);
                    break;
                case "date_desc":
                default:
                    ordersQuery = ordersQuery.OrderByDescending(x => x.Order.CreatedAt);
                    break;
            }

            // Execute the query and map to view model
            var orders = await ordersQuery
                .Select(x => new OrderStatisticsViewModel
                {
                    OrderId = x.Order.OrderId,
                    OrderNumber = $"WU-{x.Order.OrderId:D5}",
                    CustomerName = x.CustomerDetail.CustomerName,
                    CustomerEmail = x.Customer.CustomerEmail,
                    OrderDate = (DateTime)x.Order.CreatedAt,
                    TotalAmount = x.Order.TotalAmount ?? 0,
                    Status = x.Order.OrderStatus
                })
                .ToListAsync();

            // Calculate the daily earnings for the chart
            var dailyEarningsQuery = from o in _dbContext.Orders
                                     where o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.OrderStatus != "cancelled"
                                     group o by new { Date = ((DateTime)o.CreatedAt).Date } into g
                                     select new DailyEarningsViewModel
                                     {
                                         Date = g.Key.Date,
                                         Amount = g.Sum(o => o.TotalAmount ?? 0),
                                         OrderCount = g.Count()
                                     };

            var dailyEarnings = await dailyEarningsQuery.ToListAsync();

            // Calculate total statistics
            var totalOrders = orders.Count;
            var completedOrders = orders.Count(o => o.Status == "completed");
            var totalEarnings = orders.Where(o => o.Status != "cancelled").Sum(o => o.TotalAmount);
            var averageOrderValue = totalOrders > 0
                ? Math.Round(orders.Where(o => o.Status != "cancelled").Sum(o => o.TotalAmount) / Math.Max(1, completedOrders), 2)
                : 0;

            // Calculate top products
            var topProducts = await _dbContext.OrderItems
                .Where(oi => _dbContext.Orders.Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
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
                .Take(5)
                .ToListAsync();

            // Build the view model
            var viewModel = new StatisticsViewModel
            {
                Orders = orders,
                DailyEarnings = dailyEarnings,
                TotalOrders = totalOrders,
                CompletedOrders = completedOrders,
                TotalEarnings = totalEarnings,
                AverageOrderValue = averageOrderValue,
                TopProducts = topProducts,
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                Period = period ?? "custom"
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Export(DateTime startDate, DateTime endDate, string format = "csv")
        {
            // Query orders for export
            var orders = await _dbContext.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate)
                .Join(_dbContext.Customers,
                      o => o.CustomerId,
                      c => c.CustomerId,
                      (o, c) => new { Order = o, Customer = c })
                .Join(_dbContext.CustomerDetails,
                      oc => oc.Customer.CustomerId,
                      cd => cd.CustomerId,
                      (oc, cd) => new { oc.Order, oc.Customer, CustomerDetail = cd })
                .OrderByDescending(x => x.Order.CreatedAt)
                .Select(x => new OrderExportViewModel
                {
                    OrderId = x.Order.OrderId,
                    OrderNumber = $"WU-{x.Order.OrderId:D5}",
                    CustomerName = x.CustomerDetail.CustomerName,
                    CustomerEmail = x.Customer.CustomerEmail,
                    OrderDate = (DateTime)x.Order.CreatedAt,
                    TotalAmount = x.Order.TotalAmount ?? 0,
                    Status = x.Order.OrderStatus
                })
                .ToListAsync();

            // Generate filename with date range
            string fileName = $"WellUp_Orders_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}";

            if (format.ToLower() == "csv")
            {
                return ExportToCsv(orders, fileName);
            }
            else // Excel
            {
                return ExportToExcel(orders, fileName);
            }
        }

        private FileContentResult ExportToCsv(List<OrderExportViewModel> orders, string fileName)
        {
            var builder = new System.Text.StringBuilder();

            // Add headers
            builder.AppendLine("Order ID,Order Number,Customer Name,Customer Email,Order Date,Total Amount,Status");

            // Add data rows
            foreach (var order in orders)
            {
                builder.AppendLine($"{order.OrderId}," +
                                  $"\"{order.OrderNumber}\"," +
                                  $"\"{order.CustomerName}\"," +
                                  $"\"{order.CustomerEmail}\"," +
                                  $"{order.OrderDate:yyyy-MM-dd HH:mm:ss}," +
                                  $"{order.TotalAmount.ToString(CultureInfo.InvariantCulture)}," +
                                  $"{order.Status}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"{fileName}.csv");
        }

        private FileContentResult ExportToExcel(List<OrderExportViewModel> orders, string fileName)
        {
            // For a real implementation, you would use a library like EPPlus or NPOI
            // For this demo, we'll return a CSV file with Excel mime type
            var builder = new System.Text.StringBuilder();

            // Add headers
            builder.AppendLine("Order ID,Order Number,Customer Name,Customer Email,Order Date,Total Amount,Status");

            // Add data rows
            foreach (var order in orders)
            {
                builder.AppendLine($"{order.OrderId}," +
                                  $"\"{order.OrderNumber}\"," +
                                  $"\"{order.CustomerName}\"," +
                                  $"\"{order.CustomerEmail}\"," +
                                  $"{order.OrderDate:yyyy-MM-dd HH:mm:ss}," +
                                  $"{order.TotalAmount.ToString(CultureInfo.InvariantCulture)}," +
                                  $"{order.Status}");
            }

            return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{fileName}.xlsx");
        }
    }
}