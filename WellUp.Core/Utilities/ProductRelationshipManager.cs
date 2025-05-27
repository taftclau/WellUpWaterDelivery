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
        /// Updates the stock of a product and its related products
        /// </summary>
        public async Task UpdateStockWithRelationshipsAsync(
            int productId, int newQuantity, string changeReason, bool createLog = true)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return;

            int previousQuantity = product.StockQuantity ?? 0;

            // Check if this is a base product (container-only)
            if (!product.IncludesRefill && !string.IsNullOrEmpty(product.ContainerType))
            {
                // Update the base product's stock
                product.StockQuantity = newQuantity;

                // Log the change if requested
                if (createLog)
                {
                    _context.InventoryLogs.Add(new InventoryLog
                    {
                        ProductId = productId,
                        PreviousQuantity = previousQuantity,
                        NewQuantity = newQuantity,
                        ChangeReason = changeReason,
                        LoggedAt = DateTime.Now
                    });
                }

                // Now update all related products' stock to match
                var relatedProducts = await GetRelatedProductsAsync(productId);
                foreach (var relatedProduct in relatedProducts)
                {
                    int relatedPrevQuantity = relatedProduct.StockQuantity ?? 0;
                    relatedProduct.StockQuantity = newQuantity;

                    // Log the change for each related product if requested
                    if (createLog)
                    {
                        _context.InventoryLogs.Add(new InventoryLog
                        {
                            ProductId = relatedProduct.ProductId,
                            PreviousQuantity = relatedPrevQuantity,
                            NewQuantity = newQuantity,
                            ChangeReason = $"{changeReason} (Updated with base product)",
                            LoggedAt = DateTime.Now
                        });
                    }
                }
            }
            // If this is a container-with-refill product
            else if (product.IncludesRefill && !string.IsNullOrEmpty(product.ContainerType))
            {
                // Find the base product
                var baseProductId = await GetBaseProductIdAsync(productId);
                if (baseProductId.HasValue)
                {
                    // Update both this product and the base product
                    product.StockQuantity = newQuantity;

                    var baseProduct = await _context.Products.FindAsync(baseProductId.Value);
                    int basePrevQuantity = baseProduct.StockQuantity ?? 0;
                    baseProduct.StockQuantity = newQuantity;

                    // Log changes if requested
                    if (createLog)
                    {
                        _context.InventoryLogs.Add(new InventoryLog
                        {
                            ProductId = productId,
                            PreviousQuantity = previousQuantity,
                            NewQuantity = newQuantity,
                            ChangeReason = changeReason,
                            LoggedAt = DateTime.Now
                        });

                        _context.InventoryLogs.Add(new InventoryLog
                        {
                            ProductId = baseProductId.Value,
                            PreviousQuantity = basePrevQuantity,
                            NewQuantity = newQuantity,
                            ChangeReason = $"{changeReason} (Updated with refill product)",
                            LoggedAt = DateTime.Now
                        });
                    }

                    // Update other related products (other container-with-refill products)
                    var otherRelatedProducts = await _context.Products
                        .Where(p => p.ContainerType == product.ContainerType &&
                               p.IncludesRefill == true &&
                               p.ProductId != productId)
                        .ToListAsync();

                    foreach (var relatedProduct in otherRelatedProducts)
                    {
                        int relatedPrevQuantity = relatedProduct.StockQuantity ?? 0;
                        relatedProduct.StockQuantity = newQuantity;

                        if (createLog)
                        {
                            _context.InventoryLogs.Add(new InventoryLog
                            {
                                ProductId = relatedProduct.ProductId,
                                PreviousQuantity = relatedPrevQuantity,
                                NewQuantity = newQuantity,
                                ChangeReason = $"{changeReason} (Updated with related product)",
                                LoggedAt = DateTime.Now
                            });
                        }
                    }
                }
                else
                {
                    // Just update this product normally if no relationship found
                    product.StockQuantity = newQuantity;

                    if (createLog)
                    {
                        _context.InventoryLogs.Add(new InventoryLog
                        {
                            ProductId = productId,
                            PreviousQuantity = previousQuantity,
                            NewQuantity = newQuantity,
                            ChangeReason = changeReason,
                            LoggedAt = DateTime.Now
                        });
                    }
                }
            }
            // For products without relationships
            else
            {
                product.StockQuantity = newQuantity;

                if (createLog)
                {
                    _context.InventoryLogs.Add(new InventoryLog
                    {
                        ProductId = productId,
                        PreviousQuantity = previousQuantity,
                        NewQuantity = newQuantity,
                        ChangeReason = changeReason,
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