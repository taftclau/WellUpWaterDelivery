//Utilities/ProductRelationshipManager.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WellUp.Core.Data;
using WellUp.Core.Database;

namespace WellUp.Core.Utilities
{
    public class ProductRelationshipManager
    {
        // Define product relationship types
        public enum RelationshipType
        {
            None,
            ContainerWithRefill
        }

        private readonly WellUpDbContext _context;

        public ProductRelationshipManager(WellUpDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the base product ID for a given product ID based on relationship type
        /// </summary>
        public async Task<int?> GetBaseProductIdAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return null;

            // If this is a container with refill, find the matching container-only product
            if (product.IncludesRefill && !string.IsNullOrEmpty(product.ContainerType))
            {
                var baseProduct = await _context.Products
                    .Where(p => p.ContainerType == product.ContainerType &&
                           p.IncludesRefill == false)
                    .FirstOrDefaultAsync();

                return baseProduct?.ProductId;
            }

            // This is already a base product or doesn't have a relationship
            return null;
        }

        /// <summary>
        /// Gets the related products for a given base product ID
        /// </summary>
        public async Task<List<Product>> GetRelatedProductsAsync(int baseProductId)
        {
            var baseProduct = await _context.Products.FindAsync(baseProductId);
            if (baseProduct == null) return new List<Product>();

            // Only container-only products can have related products
            if (baseProduct.IncludesRefill || string.IsNullOrEmpty(baseProduct.ContainerType))
                return new List<Product>();

            // Find all container-with-refill products that match this container type
            return await _context.Products
                .Where(p => p.ContainerType == baseProduct.ContainerType &&
                       p.IncludesRefill == true)
                .ToListAsync();
        }

        /// <summary>
        /// Gets the related product based on the container type and refill status
        /// </summary>
        public async Task<Product> GetRelatedProductAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || string.IsNullOrEmpty(product.ContainerType))
                return null;

            // Find the counterpart (if this is container-only, find with-refill and vice versa)
            var relatedProduct = await _context.Products
                .Where(p => p.ContainerType == product.ContainerType &&
                       p.IncludesRefill != product.IncludesRefill)
                .FirstOrDefaultAsync();

            return relatedProduct;
        }

        /// <summary>
        /// Gets the effective stock quantity for a product, considering relationships
        /// </summary>
        public async Task<int> GetEffectiveStockQuantityAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return 0;

            // For container-with-refill products, use the stock of the base container
            if (product.IncludesRefill && !string.IsNullOrEmpty(product.ContainerType))
            {
                var baseProductId = await GetBaseProductIdAsync(productId);
                if (baseProductId.HasValue)
                {
                    var baseProduct = await _context.Products.FindAsync(baseProductId.Value);
                    return baseProduct?.StockQuantity ?? 0;
                }
            }

            // For other products, use their own stock
            return product.StockQuantity ?? 0;
        }

        /// <summary>
        /// Synchronizes stock between related gallon products (container only and with refill variants)
        /// </summary>
        public async Task SynchronizeGallonStockAsync(int productId, int quantityChange, string reason)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null || string.IsNullOrEmpty(product.ContainerType))
                return;

            // Get all products with the same container type
            var relatedProducts = await _context.Products
                .Where(p => p.ContainerType == product.ContainerType)
                .ToListAsync();

            foreach (var relatedProduct in relatedProducts)
            {
                int previousQuantity = relatedProduct.StockQuantity ?? 0;
                int newQuantity = Math.Max(0, previousQuantity + quantityChange);

                relatedProduct.StockQuantity = newQuantity;

                // Log the change
                _context.InventoryLogs.Add(new InventoryLog
                {
                    ProductId = relatedProduct.ProductId,
                    PreviousQuantity = previousQuantity,
                    NewQuantity = newQuantity,
                    ChangeReason = reason,
                    LoggedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates the stock of a product and its related products
        /// </summary>
        public async Task UpdateStockWithRelationshipsAsync(
            int productId, int newQuantity, string changeReason, bool createLog = true)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return;

            int previousQuantity = product.StockQuantity ?? 0;

            // If this is a gallon container product (with or without refill)
            if (!string.IsNullOrEmpty(product.ContainerType))
            {
                // Get all products with the same container type
                var relatedProducts = await _context.Products
                    .Where(p => p.ContainerType == product.ContainerType)
                    .ToListAsync();

                // Update all products with the same container type to have the same stock
                foreach (var relatedProduct in relatedProducts)
                {
                    int relatedPrevQuantity = relatedProduct.StockQuantity ?? 0;
                    relatedProduct.StockQuantity = newQuantity;

                    // Log the change for each related product if requested
                    if (createLog)
                    {
                        string reason = relatedProduct.ProductId == productId
                            ? changeReason
                            : $"{changeReason} (Sync #{product.ProductId})";

                        // Ensure it doesn't exceed the column length (assuming it's 100 characters)
                        if (reason.Length > 100)
                        {
                            reason = reason.Substring(0, 97) + "...";
                        }

                        _context.InventoryLogs.Add(new InventoryLog
                        {
                            ProductId = relatedProduct.ProductId,
                            PreviousQuantity = relatedPrevQuantity,
                            NewQuantity = newQuantity,
                            ChangeReason = reason,
                            LoggedAt = DateTime.Now
                        });
                    }
                }
            }
            // For non-container products (like accessories, standalone products)
            else
            {
                product.StockQuantity = newQuantity;

                if (createLog)
                {
                    // Also ensure the regular change reason isn't too long
                    string reason = changeReason;
                    if (reason.Length > 100)
                    {
                        reason = reason.Substring(0, 97) + "...";
                    }

                    _context.InventoryLogs.Add(new InventoryLog
                    {
                        ProductId = productId,
                        PreviousQuantity = previousQuantity,
                        NewQuantity = newQuantity,
                        ChangeReason = reason,
                        LoggedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Checks if a product has a relationship with another product
        /// </summary>
        public bool HasRelationship(Product product)
        {
            // A product has a relationship if:
            // 1. It's a container-with-refill product (related to a container-only product)
            // 2. It's a container-only product (related to container-with-refill products)
            return !string.IsNullOrEmpty(product.ContainerType);
        }

        /// <summary>
        /// Gets the relationship type for a product
        /// </summary>
        public RelationshipType GetRelationshipType(Product product)
        {
            if (product == null) return RelationshipType.None;

            if (!string.IsNullOrEmpty(product.ContainerType))
            {
                if (product.IncludesRefill)
                    return RelationshipType.ContainerWithRefill;
                else
                    return RelationshipType.None; // Base product
            }

            return RelationshipType.None;
        }
    }
}