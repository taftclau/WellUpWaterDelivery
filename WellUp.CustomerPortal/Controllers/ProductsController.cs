//Controllers/ProductsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WellUp.CustomerPortal.Models.ViewModels;
using WellUp.Core.Data;
using WellUp.Core.Database;
using WellUp.Core.Utilities;
using Microsoft.AspNetCore.Http;

namespace WellUp.CustomerPortal.Controllers
{
    [Authorize(Policy = "CustomerOnly")]
    public class ProductsController : Controller
    {
        private readonly WellUpDbContext _dbContext;
        private readonly ILogger<ProductsController> _logger;
        private readonly ProductRelationshipManager _productRelationshipManager;

        public ProductsController(WellUpDbContext dbContext, ILogger<ProductsController> logger,
            ProductRelationshipManager productRelationshipManager)
        {
            _dbContext = dbContext;
            _logger = logger;
            _productRelationshipManager = productRelationshipManager;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _dbContext.Products
                .Where(p => p.IsAvailable == true)
                .OrderBy(p => p.ProductId)
                .ToListAsync();

            // Create the view model with product groups
            var viewModel = new List<ProductGroupViewModel>();

            // Group products by container type
            var containerGroups = products
                .Where(p => !string.IsNullOrEmpty(p.ContainerType))
                .GroupBy(p => p.ContainerType)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var group in containerGroups)
            {
                var containerType = group.Key;
                var containerProducts = group.Value;

                var groupViewModel = new ProductGroupViewModel
                {
                    GroupName = containerType == "slim" ? "Slim Water Containers" : "Round Water Containers",
                    GroupDescription = containerType == "slim"
                        ? "Space-saving slim design water containers"
                        : "Standard round water containers",
                    ImageUrl = containerType == "slim"
                        ? "/images/products/slim_container.png"
                        : "/images/products/round_container.png",
                    Options = new List<ProductOptionViewModel>()
                };

                // Add container-only option
                var containerOnly = containerProducts.FirstOrDefault(p => !p.IncludesRefill);
                if (containerOnly != null)
                {
                    // Get effective stock quantity
                    int effectiveStock = await _productRelationshipManager.GetEffectiveStockQuantityAsync(containerOnly.ProductId);

                    groupViewModel.Options.Add(new ProductOptionViewModel
                    {
                        Product = containerOnly,
                        Name = $"{(containerType == "slim" ? "Slim" : "Round")} Container Only",
                        Description = "Container only, no water included",
                        EffectiveStockQuantity = effectiveStock
                    });
                }

                // Add container with refill option
                var containerWithRefill = containerProducts.FirstOrDefault(p => p.IncludesRefill);
                if (containerWithRefill != null)
                {
                    // Get effective stock quantity
                    int effectiveStock = await _productRelationshipManager.GetEffectiveStockQuantityAsync(containerWithRefill.ProductId);

                    groupViewModel.Options.Add(new ProductOptionViewModel
                    {
                        Product = containerWithRefill,
                        Name = $"{(containerType == "slim" ? "Slim" : "Round")} Container with Water",
                        Description = "Container with purified water included",
                        EffectiveStockQuantity = effectiveStock
                    });
                }

                viewModel.Add(groupViewModel);
            }

            // Add refill service product group (non-container products)
            var refillProducts = products
                .Where(p => p.ContainerType == null && p.IncludesRefill == true)
                .ToList();

            if (refillProducts.Any())
            {
                var refill = refillProducts.FirstOrDefault();

                viewModel.Add(new ProductGroupViewModel
                {
                    GroupName = "Water Refill",
                    GroupDescription = "Fresh, purified water refill for your own container",
                    ImageUrl = "/images/products/refill.png",
                    Options = new List<ProductOptionViewModel>
                    {
                        new ProductOptionViewModel
                        {
                            Product = refill,
                            Name = "Water Refill",
                            Description = "Customer-owned container will be used",
                            EffectiveStockQuantity = refill.StockQuantity ?? 0
                        }
                    },
                    SpecialInstructions = true
                });
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity, string notes = "")
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the product
            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            // Check effective stock quantity
            int effectiveStock = await _productRelationshipManager.GetEffectiveStockQuantityAsync(productId);
            if (effectiveStock < quantity)
            {
                TempData["ErrorMessage"] = $"Not enough stock available. Only {effectiveStock} units in stock.";
                return RedirectToAction(nameof(Index));
            }

            // Get or create cart session
            var cartItems = HttpContext.Session.Get<List<CartItemViewModel>>("CartItems") ?? new List<CartItemViewModel>();

            // Check if product already exists in cart
            var existingItem = cartItems.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                // Update quantity
                existingItem.Quantity += quantity;
                // Update notes if provided
                if (!string.IsNullOrEmpty(notes))
                {
                    existingItem.Notes = notes;
                }
                // Update subtotal
                existingItem.SubTotal = existingItem.UnitPrice * existingItem.Quantity;
            }
            else
            {
                // Add new item to cart
                cartItems.Add(new CartItemViewModel
                {
                    ProductId = productId,
                    ProductName = product.ProductName,
                    ImagePath = product.ImagePath ?? $"/images/products/{(product.ContainerType ?? "refill")}.png",
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    SubTotal = product.Price * quantity,
                    Notes = notes,
                    IsRefillService = product.ContainerType == null && product.IncludesRefill,
                    StockQuantity = effectiveStock,
                    ContainerType = product.ContainerType
                });
            }

            // Save cart back to session
            HttpContext.Session.Set("CartItems", cartItems);

            TempData["SuccessMessage"] = $"{quantity} {product.ProductName} added to your cart!";

            // Redirect back to product listing
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyNow(int productId, int quantity, string notes = "")
        {
            // Get current customer ID from claims
            if (!int.TryParse(User.FindFirstValue("CustomerId"), out int customerId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the product
            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            // Check effective stock quantity
            int effectiveStock = await _productRelationshipManager.GetEffectiveStockQuantityAsync(productId);
            if (effectiveStock < quantity)
            {
                TempData["ErrorMessage"] = $"Not enough stock available. Only {effectiveStock} units in stock.";
                return RedirectToAction(nameof(Index));
            }

            // Store the product info in TempData to pass to the order summary page
            TempData["BuyNowProduct"] = new
            {
                ProductId = productId,
                ProductName = product.ProductName,
                Description = product.Description,
                ImagePath = product.ImagePath ?? $"/images/products/{(product.ContainerType ?? "refill")}.png",
                UnitPrice = product.Price,
                Quantity = quantity,
                Notes = notes,
                IsRefillService = product.ContainerType == null && product.IncludesRefill
            };

            // Redirect to order summary page
            return RedirectToAction("Summary", "Orders");
        }
    }
}