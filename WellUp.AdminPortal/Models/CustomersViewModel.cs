//CustomersViewModel.cs
using System;
using System.Collections.Generic;

namespace WellUp.AdminPortal.Models.ViewModels
{
    public class CustomersListViewModel
    {
        public List<CustomerListItemViewModel> Customers { get; set; } = new List<CustomerListItemViewModel>();
        public int ComplaintCount { get; set; }
    }

    public class CustomerListItemViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? DateCreated { get; set; }
        public int OrderCount { get; set; }
    }

    public class CustomerDetailsViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? DateCreated { get; set; }
        public List<AddressViewModel> Addresses { get; set; } = new List<AddressViewModel>();
        public List<OrderHistoryViewModel> Orders { get; set; } = new List<OrderHistoryViewModel>();
        public List<FeedbackViewModel> Feedback { get; set; } = new List<FeedbackViewModel>();
    }

    public class AddressViewModel
    {
        public int AddressId { get; set; }
        public string CityMunicipality { get; set; }
        public string Barangay { get; set; }
        public string Street { get; set; }
        public string ZipCode { get; set; }
        public bool IsDefault { get; set; }

        public string FullAddress => $"{Street}, {Barangay}, {CityMunicipality} {ZipCode}";
    }

    public class OrderHistoryViewModel
    {
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
    }

    public class FeedbackViewModel
    {
        public int FeedbackId { get; set; }
        public int OrderId { get; set; }
        public string FeedbackType { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class FeedbackListViewModel
    {
        public int FeedbackId { get; set; }
        public string CustomerName { get; set; }
        public int CustomerId { get; set; }
        public int OrderId { get; set; }
        public string FeedbackType { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedAt { get; set; } // Note: Changed to nullable DateTime?
        public string Email { get; set; }
    }

    public class FeedbackDetailsViewModel
    {
        public int FeedbackId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public int OrderId { get; set; }
        public DateTime? OrderDate { get; set; } // Note: Changed to nullable DateTime?
        public string OrderStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public string FeedbackType { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedAt { get; set; } // Note: Changed to nullable DateTime?
        public string DeliveryAddress { get; set; }
        public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();

        // Format status property for easier display
        public string FormattedOrderStatus => OrderStatus switch
        {
            "new" => "New",
            "in_progress" => "In Progress",
            "completed" => "Completed",
            "cancelled" => "Cancelled",
            _ => OrderStatus
        };
    }

}