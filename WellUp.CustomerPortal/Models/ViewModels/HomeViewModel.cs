// WellUp.CustomerPortal.Models.ViewModels/HomeViewModel.cs
using System.Collections.Generic;

namespace WellUp.CustomerPortal.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<ProductViewModel> FeaturedProducts { get; set; } = new List<ProductViewModel>();
    }

    public class ProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; }
        public string ContainerType { get; set; }
        public bool IncludesRefill { get; set; }
        public bool IsAvailable { get; set; }
    }
}