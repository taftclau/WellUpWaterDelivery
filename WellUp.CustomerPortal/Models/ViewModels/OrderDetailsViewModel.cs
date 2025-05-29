// WellUp.CustomerPortal/Models/ViewModels/OrderDetailsViewModel.cs

using System;
using System.Collections.Generic;
using System.Linq;
using WellUp.Core.Database;

namespace WellUp.CustomerPortal.Models.ViewModels
{
    public class OrderDetailsViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public Address DeliveryAddress { get; set; }
        public DateTime? PreferredDeliveryTime { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string DeliveryStatus { get; set; }
        public DateTime? ScheduledDeliveryTime { get; set; }
        public string DeliveryNotes { get; set; }
        public List<OrderItemDetailsViewModel> Items { get; set; } = new List<OrderItemDetailsViewModel>();
        public bool CanReorder { get; set; }
        public bool CanCancel { get; set; }
        public bool HasFeedback { get; set; }

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

        // Helper method to check if all items are available for reorder
        public bool AllItemsAvailableForReorder()
        {
            return Items.All(i => i.IsAvailableForReorder);
        }
    }

    public class OrderItemDetailsViewModel
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImagePath { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public bool IsAvailableForReorder { get; set; }
        public string ContainerType { get; set; }
        public bool IncludesRefill { get; set; }
    }
}