//InventoryViewModel.cs

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using WellUp.Core.Database;
using WellUp.Core.Utilities;

namespace WellUp.AdminPortal.Models.ViewModels
{
    // View model for product listing
    public class ProductViewModel
    {
        public int ProductId { get; set; }

        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Container Type")]
        public string? ContainerType { get; set; }

        [Display(Name = "Includes Refill")]
        public bool IncludesRefill { get; set; }

        [Display(Name = "Price")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Display(Name = "In Stock")]
        public int StockQuantity { get; set; }

        [Display(Name = "Low Stock Alert")]
        public int LowStockThreshold { get; set; }

        [Display(Name = "Status")]
        public bool IsAvailable { get; set; }

        // Image properties
        [Display(Name = "Image Path")]
        public string? ImagePath { get; set; }

        // Image-related properties
        [Display(Name = "Product Image")]
        public IFormFile? ProductImage { get; set; }  // Make sure it has the ? to mark it as nullable

        [Display(Name = "Default Image")]
        public string? DefaultImageId { get; set; }

        // For UI display
        public List<ProductImageOption> DefaultImageOptions { get; set; } = DefaultProductImages.Options;

        // Calculated property to determine stock status
        public string StockStatus
        {
            get
            {
                if (StockQuantity <= 0)
                    return "out_of_stock";
                if (StockQuantity <= 5)
                    return "critical";
                if (StockQuantity <= LowStockThreshold)
                    return "low";
                return "normal";
            }
        }

        public string FormattedStockStatus
        {
            get
            {
                return StockStatus switch
                {
                    "out_of_stock" => "Out of Stock",
                    "critical" => "Critical Stock",
                    "low" => "Low Stock",
                    _ => "In Stock"
                };
            }
        }

        // Method to convert from database model to view model
        public static ProductViewModel FromProduct(Product product)
        {
            var model = new ProductViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Description = product.Description ?? "",
                ContainerType = product.ContainerType,
                IncludesRefill = product.IncludesRefill,
                Price = product.Price,
                StockQuantity = product.StockQuantity ?? 0,
                LowStockThreshold = product.LowStockThreshold ?? 5,
                IsAvailable = product.IsAvailable ?? true,
                ImagePath = product.ImagePath,
                DefaultImageOptions = DefaultProductImages.Options
            };

            // Try to match to a default image
            if (!string.IsNullOrEmpty(product.ImagePath))
            {
                foreach (var option in DefaultProductImages.Options)
                {
                    if (product.ImagePath == option.Path)
                    {
                        model.DefaultImageId = option.Id;
                        break;
                    }
                }
            }

            return model;
        }

        // Method to update database model from view model
        public void UpdateProduct(Product product)
        {
            product.ProductName = ProductName;
            product.Description = Description;
            product.ContainerType = ContainerType;
            product.IncludesRefill = IncludesRefill;
            product.Price = Price;
            product.StockQuantity = StockQuantity;
            product.LowStockThreshold = LowStockThreshold;
            product.IsAvailable = IsAvailable;
        }
    }

    // Enhanced view model for adding a new product
    // Replace just the AddProductViewModel class in your InventoryViewModels.cs file
    public class AddProductViewModel
    {
        [Required(ErrorMessage = "Product name is required")]
        [Display(Name = "Product Name")]
        [StringLength(100)]
        public string ProductName { get; set; }

        [Display(Name = "Description")]
        [StringLength(255)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Display(Name = "Price")]
        [DataType(DataType.Currency)]
        [Range(0.01, 10000, ErrorMessage = "Price must be between 0.01 and 10000")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Initial stock is required")]
        [Display(Name = "Initial Stock")]
        [Range(0, 100000, ErrorMessage = "Stock must be between 0 and 100000")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Low stock threshold is required")]
        [Display(Name = "Low Stock Alert")]
        [Range(1, 10000, ErrorMessage = "Threshold must be between 1 and 10000")]
        public int LowStockThreshold { get; set; } = 10;

        [Display(Name = "Available for Sale")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Container Type")]
        public string ContainerType { get; set; }

        [Display(Name = "Includes Refill")]
        public bool IncludesRefill { get; set; }

        // Image-related properties
        [Display(Name = "Product Image")]
        public IFormFile ProductImage { get; set; }

        [Display(Name = "Default Image")]
        public string DefaultImageId { get; set; } = "slim_container";

        // For UI display
        public List<ProductImageOption> DefaultImageOptions { get; set; } = DefaultProductImages.Options;

        // Method to generate default product name
        private string GenerateDefaultName()
        {
            if (ContainerType == "slim")
            {
                return IncludesRefill ? "Slim Gallon with Refill" : "Slim Gallon Container Only";
            }
            else if (ContainerType == "round")
            {
                return IncludesRefill ? "Round Gallon with Refill" : "Round Gallon Container Only";
            }
            else if (IncludesRefill && string.IsNullOrEmpty(ContainerType))
            {
                return "Water Refill Service";
            }
            return "Container";
        }

        // Create a new Product from this view model
        public Product ToProduct()
        {
            string productName = string.IsNullOrEmpty(ProductName) ? GenerateDefaultName() : ProductName;

            var product = new Product
            {
                ProductName = productName,
                Description = Description,
                ContainerType = string.IsNullOrEmpty(ContainerType) ? null : ContainerType, // Handle null/empty case
                IncludesRefill = IncludesRefill,
                Price = Price,
                StockQuantity = StockQuantity,
                LowStockThreshold = LowStockThreshold,
                IsAvailable = IsAvailable
            };

            // Set ImagePath based on DefaultImageId
            if (!string.IsNullOrEmpty(DefaultImageId))
            {
                var selectedDefaultImage = DefaultProductImages.Options.FirstOrDefault(o => o.Id == DefaultImageId);
                if (selectedDefaultImage != null)
                {
                    product.ImagePath = selectedDefaultImage.Path;
                }
            }

            return product;
        }
    }

    // New Edit Product View Model
    public class EditProductViewModel
    {
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Product Type")]
        public string ProductType { get; set; }

        [Display(Name = "Product Variant")]
        public string ProductVariant { get; set; }

        [Display(Name = "Product Name")]
        [StringLength(100)]
        public string ProductName { get; set; }

        [Display(Name = "Description")]
        [StringLength(255)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Price")]
        [DataType(DataType.Currency)]
        [Range(0.01, 10000)]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Stock")]
        [Range(0, 100000)]
        public int StockQuantity { get; set; }

        [Required]
        [Display(Name = "Low Stock Alert")]
        [Range(1, 10000)]
        public int LowStockThreshold { get; set; } = 5;

        [Display(Name = "Available for Sale")]
        public bool IsAvailable { get; set; } = true;

        // Image-related properties
        public string? ImagePath { get; set; }

        [Display(Name = "Product Image")]
        public IFormFile? ProductImage { get; set; }

        [Display(Name = "Default Image")]
        public string? DefaultImageId { get; set; }

        // For UI display
        public List<ProductImageOption> DefaultImageOptions { get; set; } = DefaultProductImages.Options;

        // Create from existing product
        public static EditProductViewModel FromProduct(Product product)
        {
            var model = new EditProductViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Description = product.Description ?? "",
                Price = product.Price,
                StockQuantity = product.StockQuantity ?? 0,
                LowStockThreshold = product.LowStockThreshold ?? 5,
                IsAvailable = product.IsAvailable ?? true,
                ImagePath = product.ImagePath,
                DefaultImageOptions = DefaultProductImages.Options
            };

            // Set product type and variant based on container type and refill status
            if (product.ContainerType == "slim")
            {
                model.ProductType = "slim_gallon";
                model.ProductVariant = product.IncludesRefill ? "with_refill" : "container_only";
                model.DefaultImageId = "slim_container";
            }
            else if (product.ContainerType == "round")
            {
                model.ProductType = "round_gallon";
                model.ProductVariant = product.IncludesRefill ? "with_refill" : "container_only";
                model.DefaultImageId = "round_container";
            }
            else if (product.IncludesRefill)
            {
                model.ProductType = "water_refill";
                model.DefaultImageId = "refill";
            }

            // Override default image ID if a matching default image is found
            if (!string.IsNullOrEmpty(product.ImagePath))
            {
                foreach (var option in DefaultProductImages.Options)
                {
                    if (product.ImagePath == option.Path)
                    {
                        model.DefaultImageId = option.Id;
                        break;
                    }
                }
            }

            return model;
        }

        // Update existing product from this view model
        public void UpdateProduct(Product product)
        {
            product.ProductName = ProductName;
            product.Description = Description;
            product.Price = Price;
            product.StockQuantity = StockQuantity;
            product.LowStockThreshold = LowStockThreshold;
            product.IsAvailable = IsAvailable;

            // Update container type and refill status based on product type
            switch (ProductType)
            {
                case "slim_gallon":
                    product.ContainerType = "slim";
                    product.IncludesRefill = ProductVariant == "with_refill";
                    break;

                case "round_gallon":
                    product.ContainerType = "round";
                    product.IncludesRefill = ProductVariant == "with_refill";
                    break;

                case "water_refill":
                    product.ContainerType = null;
                    product.IncludesRefill = true;
                    break;
            }
        }
    }

    // View model for updating stock
    public class StockUpdateViewModel
    {
        public int ProductId { get; set; }

        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        [Display(Name = "Current Stock")]
        public int CurrentStock { get; set; }

        [Required]
        [Display(Name = "New Stock Level")]
        [Range(0, 100000)]
        public int NewStock { get; set; }

        [Required]
        [Display(Name = "Reason for Change")]
        [StringLength(50)]
        public string ChangeReason { get; set; }
    }

    // View model for inventory listing page
    public class InventoryListViewModel
    {
        public List<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public List<InventoryLogViewModel> InventoryLogs { get; set; } = new List<InventoryLogViewModel>(); // New property
    }

    // Add a new view model for inventory logs
    public class InventoryLogViewModel
    {
        public int LogId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int PreviousQuantity { get; set; }
        public int NewQuantity { get; set; }
        public string ChangeReason { get; set; } = string.Empty;
        public DateTime LoggedAt { get; set; }
    }

    // View model for low stock page
    public class LowStockViewModel
    {
        public List<ProductViewModel> LowStockProducts { get; set; } = new List<ProductViewModel>();
        public int CriticalStockCount { get; set; } // 5 or less
        public int LowStockCount { get; set; } // Less than threshold
    }

    public class DeleteConfirmationViewModel
    {
        public int ProductId { get; set; }

        [Display(Name = "Product Name")]
        public string ProductName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Admin Password")]
        public string Password { get; set; }
    }
}