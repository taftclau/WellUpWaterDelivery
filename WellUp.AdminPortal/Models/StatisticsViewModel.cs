//StatisticsViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WellUp.AdminPortal.Models.ViewModels
{
    public class StatisticsViewModel
    {
        public List<OrderStatisticsViewModel> Orders { get; set; } = new List<OrderStatisticsViewModel>();
        public List<DailyEarningsViewModel> DailyEarnings { get; set; } = new List<DailyEarningsViewModel>();
        public List<TopProductViewModel> TopProducts { get; set; } = new List<TopProductViewModel>();

        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal AverageOrderValue { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Period { get; set; } = "month";

        public string FormattedDateRange
        {
            get
            {
                if (StartDate.Date == EndDate.Date)
                {
                    return StartDate.ToString("MMMM dd, yyyy");
                }
                else if (StartDate.Month == EndDate.Month && StartDate.Year == EndDate.Year)
                {
                    return $"{StartDate.ToString("MMMM dd")} - {EndDate.ToString("dd, yyyy")}";
                }
                else
                {
                    return $"{StartDate.ToString("MMM dd, yyyy")} - {EndDate.ToString("MMM dd, yyyy")}";
                }
            }
        }
    }

    public class OrderStatisticsViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }

        public string FormattedStatus
        {
            get
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
    }

    public class DailyEarningsViewModel
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public int OrderCount { get; set; }
    }

    public class TopProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Revenue { get; set; }
    }

    public class OrderExportViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }
}