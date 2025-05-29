// WellUp.CustomerPortal/Models/ViewModels/OrderHistoryViewModel.cs

using System;
using System.Collections.Generic;
using WellUp.Core.Database;

namespace WellUp.CustomerPortal.Models.ViewModels
{
    public class OrderHistoryViewModel
    {
        // Updated property types to HistorySummaryViewModel
        public List<HistorySummaryViewModel> CurrentOrders { get; set; } = new List<HistorySummaryViewModel>();
        public List<HistorySummaryViewModel> PastOrders { get; set; } = new List<HistorySummaryViewModel>();
    }

    public class HistorySummaryViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string DeliveryStatus { get; set; }
        public bool CanReorder { get; set; }
        public bool CanCancel { get; set; }

        // Helper property to get status badge class
        public string GetStatusBadgeClass()
        {
            return OrderStatus?.ToLower() switch
            {
                "new" => "bg-info",
                "in_progress" => "bg-primary",
                "completed" => "bg-success",
                "cancelled" => "bg-danger",
                _ => "bg-secondary"
            };
        }

        // Helper property to get delivery status badge class
        public string GetDeliveryBadgeClass()
        {
            return DeliveryStatus?.ToLower() switch
            {
                "pending" => "bg-warning text-dark",
                "out_for_delivery" => "bg-primary",
                "completed" => "bg-success",
                "failed" => "bg-danger",
                "scheduled" => "bg-info",
                _ => "bg-secondary"
            };
        }

        // Helper method to get formatted status text
        public string GetFormattedStatus()
        {
            return OrderStatus?.Replace("_", " ")?.ToUpperInvariant() ?? "UNKNOWN";
        }

        // Helper method to get formatted delivery status text
        public string GetFormattedDeliveryStatus()
        {
            return DeliveryStatus?.Replace("_", " ")?.ToUpperInvariant() ?? "UNKNOWN";
        }
    }
}