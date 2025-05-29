// WellUp.CustomerPortal/Models/ViewModels/CartViewModel.cs

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using WellUp.Core.Database;

namespace WellUp.CustomerPortal.Models.ViewModels
{
    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal TotalAmount => Items.Sum(i => i.SubTotal);
        public int ItemCount => Items.Sum(i => i.Quantity);
        public bool HasItems => Items.Any();

        // New properties for checkout
        public List<Address> AvailableAddresses { get; set; } = new List<Address>();

        [Required(ErrorMessage = "Please select a delivery address")]
        public int AddressId { get; set; }

        [Required(ErrorMessage = "Please select a delivery time")]
        public DateTime PreferredDeliveryTime { get; set; }

        // Delivery time constraints
        public DateTime EarliestDeliveryTime { get; set; }
        public DateTime LatestDeliveryTime { get; set; }
        public bool IsSameDayAvailable { get; set; }
    }

    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public string Notes { get; set; }
        public string ImagePath { get; set; }
        public bool IsRefillService { get; set; }
        public int StockQuantity { get; set; }
        public string ContainerType { get; set; }
    }
}