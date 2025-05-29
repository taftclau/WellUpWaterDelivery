//OrderSummaryViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WellUp.Core.Database;

namespace WellUp.CustomerPortal.Models.ViewModels
{
    public class OrderSummaryViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public string ImagePath { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public bool IsRefillService { get; set; }

        [Display(Name = "Special Instructions")]
        public string Notes { get; set; }

        [Required(ErrorMessage = "Please select a delivery address")]
        [Display(Name = "Delivery Address")]
        public int AddressId { get; set; }

        public List<Address> AvailableAddresses { get; set; } = new List<Address>();

        [Required(ErrorMessage = "Please select a delivery time")]
        [Display(Name = "Preferred Delivery Time")]
        [DataType(DataType.DateTime)]
        public DateTime PreferredDeliveryTime { get; set; }

        // Delivery time constraints
        public DateTime EarliestDeliveryTime { get; set; }
        public DateTime LatestDeliveryTime { get; set; }

        // For same-day delivery cutoff
        public bool IsSameDayAvailable { get; set; }
    }
}
