// DashboardViewModel.cs

using System;
using System.Collections.Generic;

namespace WellUp.AdminPortal.Models.ViewModels
{
    public class DashboardViewModel
    {
        // Summary statistics
        public int TotalOrders { get; set; }
        public int NewOrders { get; set; }
        public int PendingDeliveries { get; set; }
        public int LowStockProducts { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalCustomers { get; set; }

        // Collections
        public List<OrderViewModel> RecentOrders { get; set; } = new List<OrderViewModel>();
        public List<ActivityViewModel> RecentActivities { get; set; } = new List<ActivityViewModel>();
        public List<DashboardProductViewModel> LowStockItems { get; set; } = new List<DashboardProductViewModel>();
    }

    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber => $"WU-{OrderId:D5}";
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public string FormattedStatus => GetFormattedStatus();

        private string GetFormattedStatus()
        {
            return Status switch
            {
                "new" => "New",
                "in_progress" => "In Progress",
                "completed" => "Completed",
                "cancelled" => "Cancelled",
                _ => Status
            };
        }
    }

    public class ActivityViewModel
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; }
        public string IconClass { get; set; }
    }

    public class DashboardProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int StockQuantity { get; set; }
        public int LowStockThreshold { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}