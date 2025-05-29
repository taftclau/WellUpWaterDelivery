// WellUp.CustomerPortal/Models/ViewModels/ProductViewModel.cs

using System.Collections.Generic;
using WellUp.Core.Database;

namespace WellUp.CustomerPortal.Models.ViewModels
{
    public class ProductGroupViewModel
    {
        public string GroupName { get; set; }
        public string GroupDescription { get; set; }
        public string ImageUrl { get; set; }
        public List<ProductOptionViewModel> Options { get; set; }
        public bool SpecialInstructions { get; set; } = false;
    }

    public class ProductOptionViewModel
    {
        public Product Product { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int EffectiveStockQuantity { get; set; }
    }
}