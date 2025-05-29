//OrderViewModels.cs
using System;
using System.Collections.Generic;

namespace WellUp.AdminPortal.Models.ViewModels
{
    public class OrdersViewModel
    {
        public List<OrderListViewModel> ActiveOrders { get; set; } = new List<OrderListViewModel>();
        public List<OrderListViewModel> CompletedOrders { get; set; } = new List<OrderListViewModel>();
        public List<TopProductViewModel> TopProducts { get; set; } = new List<TopProductViewModel>(); 
    }

    public class OrderListViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber => $"WU-{OrderId:D5}";
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string DeliveryAddress { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime? PreferredDeliveryTime { get; set; }

        // Delivery properties
        public int? DeliveryId { get; set; }
        public string DeliveryStatus { get; set; }
        public DateTime? ScheduledTime { get; set; }

        public string FormattedStatus => FormatStatus(Status);
        public string FormattedDeliveryStatus => FormatDeliveryStatus(DeliveryStatus);

        private string FormatStatus(string status)
        {
            return status switch
            {
                "new" => "New",
                "in_progress" => "In Progress",
                "completed" => "Completed",
                "cancelled" => "Cancelled",
                _ => status ?? "New"
            };
        }

        private string FormatDeliveryStatus(string status)
        {
            return status switch
            {
                "pending" => "Pending",
                "scheduled" => "Scheduled",
                "out_for_delivery" => "Out for Delivery",
                "completed" => "Completed",
                "failed" => "Failed",
                _ => status ?? "Pending"
            };
        }
    }

    public class OrderDetailViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber => $"WU-{OrderId:D5}";
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string DeliveryAddress { get; set; }
        public int CustomerId { get; set; }
        public int AddressId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime? PreferredDeliveryTime { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        public string FormattedStatus => FormatStatus(Status);
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();
        public DeliveryViewModel Delivery { get; set; }

        private string FormatStatus(string status)
        {
            return status switch
            {
                "new" => "New",
                "in_progress" => "In Progress",
                "completed" => "Completed",
                "cancelled" => "Cancelled",
                _ => status ?? "New"
            };
        }
    }

    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class DeliveryViewModel
    {
        public int DeliveryId { get; set; }
        public string Status { get; set; }
        public DateTime? ScheduledTime { get; set; }
        public string Notes { get; set; }

        public string FormattedStatus => FormatStatus(Status);

        private string FormatStatus(string status)
        {
            return status switch
            {
                "pending" => "Pending",
                "scheduled" => "Scheduled",
                "out_for_delivery" => "Out for Delivery",
                "completed" => "Completed",
                "failed" => "Failed",
                _ => status ?? "Pending"
            };
        }
    }

}