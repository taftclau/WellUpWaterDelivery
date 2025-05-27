//InventoryController

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WellUp.AdminPortal.Models.ViewModels;
using WellUp.Core.Data;
using WellUp.Core.Database;
using WellUp.Core.Utilities;

namespace WellUp.AdminPortal.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class InventoryController : Controller
    {
        private readonly WellUpDbContext _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ProductRelationshipManager _relationshipManager;


        public InventoryController(WellUpDbContext dbContext, IWebHostEnvironment webHostEnvironment, ProductRelationshipManager relationshipManager)
        {
            _dbContext = dbContext;
            _webHostEnvironment = webHostEnvironment;
            _relationshipManager = relationshipManager;
        }

        // GET: Inventory
        public async Task<IActionResult> Index(string sortOrder = null, string searchString = null, string filter = null)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParam"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["StockSortParam"] = sortOrder == "stock" ? "stock_desc" : "stock";
            ViewData["PriceSortParam"] = sortOrder == "price" ? "price_desc" : "price";
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentFilterType"] = filter;

            var model = new InventoryListViewModel();

            // Get all products from database
            IQueryable<Product> productsQuery = _dbContext.Products;

            // Apply search filter
            if (!String.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p =>
                    p.ProductName.Contains(searchString) ||
                    (p.Description != null && p.Description.Contains(searchString)));
            }

            // Apply type filter
            if (!String.IsNullOrEmpty(filter))
            {
                if (filter == "all")
                {
                    // No filter
                }
                else if (filter == "low_stock")
                {
                    productsQuery = productsQuery.Where(p => p.StockQuantity <= p.LowStockThreshold);
                }
                else if (filter == "out_of_stock")
                {
                    productsQuery = productsQuery.Where(p => p.StockQuantity <= 0);
                }
                else if (filter == "available")
                {
                    productsQuery = productsQuery.Where(p => p.IsAvailable == true);
                }
                else if (filter == "unavailable")
                {
                    productsQuery = productsQuery.Where(p => p.IsAvailable == false);
                }
                else if (filter == "container_round")
                {
                    productsQuery = productsQuery.Where(p => p.ContainerType == "round");
                }
                else if (filter == "container_slim")
                {
                    productsQuery = productsQuery.Where(p => p.ContainerType == "slim");
                }
                else if (filter == "refill")
                {
                    productsQuery = productsQuery.Where(p => p.IncludesRefill == true);
                }
            }

            // Apply sorting
            productsQuery = sortOrder switch
            {
                "name_desc" => productsQuery.OrderByDescending(p => p.ProductName),
                "stock" => productsQuery.OrderBy(p => p.StockQuantity),
                "stock_desc" => productsQuery.OrderByDescending(p => p.StockQuantity),
                "price" => productsQuery.OrderBy(p => p.Price),
                "price_desc" => productsQuery.OrderByDescending(p => p.Price),
                _ => productsQuery.OrderBy(p => p.ProductName),
            };

            // Execute query and map to view models
            var products = await productsQuery
                .Select(p => ProductViewModel.FromProduct(p))
                .ToListAsync();

            // Set model properties
            model.Products = products;
            model.TotalProducts = products.Count;
            model.LowStockProducts = products.Count(p => p.StockStatus == "low" || p.StockStatus == "critical");
            model.OutOfStockProducts = products.Count(p => p.StockStatus == "out_of_stock");

            // Add this to load recent inventory logs
            var recentLogs = await _dbContext.InventoryLogs
                .Include(l => l.Product)
                .OrderByDescending(l => l.LoggedAt)
                .Take(10)  // Get the 10 most recent logs
                .Select(l => new InventoryLogViewModel
                {
                    LogId = l.LogId,
                    ProductId = l.ProductId,
                    ProductName = l.Product.ProductName,
                    PreviousQuantity = l.PreviousQuantity,
                    NewQuantity = l.NewQuantity,
                    ChangeReason = l.ChangeReason,
                    LoggedAt = l.LoggedAt ?? DateTime.Now
                })
                .ToListAsync();

            model.InventoryLogs = recentLogs;  // Add the logs to your view model

            return View(model);
        }

        // GET: Inventory/LowStock
        public async Task<IActionResult> LowStock()
        {
            var model = new LowStockViewModel();

            // Get low stock products
            var products = await _dbContext.Products
                .Where(p => p.StockQuantity <= p.LowStockThreshold)
                .OrderBy(p => p.StockQuantity)
                .Select(p => ProductViewModel.FromProduct(p))
                .ToListAsync();

            model.LowStockProducts = products;
            model.CriticalStockCount = products.Count(p => p.StockQuantity <= 5);
            model.LowStockCount = products.Count;

            return View(model);
        }

        // GET: Inventory/AddProduct
        public IActionResult AddProduct()
        {
            var model = new AddProductViewModel
            {
                DefaultImageOptions = DefaultProductImages.Options,
                IsAvailable = true,
                LowStockThreshold = 10
            };
            return View(model);
        }

        // POST: Inventory/AddProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(AddProductViewModel model)
        {
            // Explicitly remove validation errors for ProductImage
            ModelState.Remove("ProductImage");

            // Check if container type is selected
            if (string.IsNullOrEmpty(model.ContainerType))
            {
                ModelState.AddModelError("ContainerType", "Please select a container type or \"No Container\"");
            }

            // Special handling for "null" string value to convert to null
            if (model.ContainerType == "null")
            {
                model.ContainerType = null;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Convert view model to product
                    var product = model.ToProduct();

                    // If custom image is uploaded, it takes precedence over default image
                    if (model.ProductImage != null && model.ProductImage.Length > 0)
                    {
                        try
                        {
                            // Create unique filename
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ProductImage.FileName);

                            // Determine upload path
                            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", "custom");

                            // Ensure directory exists
                            if (!Directory.Exists(uploadsFolder))
                            {
                                Directory.CreateDirectory(uploadsFolder);
                            }

                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            // Save the file
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await model.ProductImage.CopyToAsync(fileStream);
                            }

                            // Save the custom path to the database (overriding any default image)
                            product.ImagePath = "/images/products/custom/" + uniqueFileName;
                        }
                        catch (Exception ex)
                        {
                            // If image processing fails, use the default image instead
                            ModelState.AddModelError("ProductImage", "Error processing image: " + ex.Message);
                            // But don't prevent product creation
                        }
                    }

                    // Add default image if none is provided
                    if (string.IsNullOrEmpty(product.ImagePath))
                    {
                        // Assign a default image based on product type
                        if (model.ContainerType == "slim")
                        {
                            product.ImagePath = "/images/products/slim_container.png";
                        }
                        else if (model.ContainerType == "round")
                        {
                            product.ImagePath = "/images/products/round_container.png";
                        }
                        else
                        {
                            // For "No Container" or if something goes wrong
                            product.ImagePath = "/images/products/refill.png";
                        }
                    }

                    // Add to database
                    _dbContext.Products.Add(product);
                    await _dbContext.SaveChangesAsync();

                    // Add inventory log for initial stock
                    if (product.StockQuantity > 0)
                    {
                        var inventoryLog = new InventoryLog
                        {
                            ProductId = product.ProductId,
                            PreviousQuantity = 0,
                            NewQuantity = product.StockQuantity.Value,
                            ChangeReason = "Initial stock",
                            LoggedAt = DateTime.Now
                        };

                        _dbContext.InventoryLogs.Add(inventoryLog);
                        await _dbContext.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = $"Product '{product.ProductName}' has been added successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Error creating product: {ex.Message}";

                    // Include inner exception details if available
                    if (ex.InnerException != null)
                    {
                        errorMessage += $" Inner exception: {ex.InnerException.Message}";
                    }

                    ModelState.AddModelError("", errorMessage);
                }
            }

            // If we got to here, something failed, redisplay form
            model.DefaultImageOptions = DefaultProductImages.Options;
            return View(model);
        }

        // GET: Inventory/EditProduct/5
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _dbContext.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var productViewModel = ProductViewModel.FromProduct(product);
            return View(productViewModel);
        }

        // POST: Inventory/EditProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, ProductViewModel model)
        {
            if (id != model.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var product = await _dbContext.Products.FindAsync(id);
                    if (product == null)
                    {
                        return NotFound();
                    }

                    // Track old stock for inventory log
                    int oldStock = product.StockQuantity ?? 0;

                    // Update product from view model
                    model.UpdateProduct(product);

                    // Handle image updates
                    if (model.ProductImage != null && model.ProductImage.Length > 0)
                    {
                        // Delete old custom image if it exists and isn't a default image
                        if (!string.IsNullOrEmpty(product.ImagePath) &&
                            !product.ImagePath.StartsWith("/images/products/slim_container") &&
                            !product.ImagePath.StartsWith("/images/products/round_container") &&
                            !product.ImagePath.StartsWith("/images/products/refill"))
                        {
                            string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Create unique filename
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ProductImage.FileName);

                        // Determine upload path
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", "custom");

                        // Ensure directory exists
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Save the file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.ProductImage.CopyToAsync(fileStream);
                        }

                        // Save the custom path to the database
                        product.ImagePath = "/images/products/custom/" + uniqueFileName;
                    }
                    else if (!string.IsNullOrEmpty(model.DefaultImageId))
                    {
                        // Use selected default image
                        var selectedDefaultImage = DefaultProductImages.Options.FirstOrDefault(o => o.Id == model.DefaultImageId);
                        if (selectedDefaultImage != null)
                        {
                            product.ImagePath = selectedDefaultImage.Path;
                        }
                    }

                    _dbContext.Update(product);
                    await _dbContext.SaveChangesAsync();

                    // Add log if stock changed
                    if (oldStock != product.StockQuantity)
                    {
                        var inventoryLog = new InventoryLog
                        {
                            ProductId = product.ProductId,
                            PreviousQuantity = oldStock,
                            NewQuantity = product.StockQuantity.Value,
                            ChangeReason = "Product edit",
                            LoggedAt = DateTime.Now
                        };

                        _dbContext.InventoryLogs.Add(inventoryLog);
                        await _dbContext.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = $"Product '{product.ProductName}' has been updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(model.ProductId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }

            return View(model);
        }

        // GET: Inventory/StockUpdate/5
        public async Task<IActionResult> StockUpdate(int id)
        {
            var product = await _dbContext.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Get the effective stock quantity based on relationships
            int effectiveStock = await _relationshipManager.GetEffectiveStockQuantityAsync(id);

            var model = new StockUpdateViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                CurrentStock = effectiveStock,  // Use effective stock
                NewStock = effectiveStock       // Default to current stock
            };

            return View(model);
        }

        // POST: Inventory/StockUpdate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockUpdate(StockUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Use relationship manager to update stock for both the product and any related products
                await _relationshipManager.UpdateStockWithRelationshipsAsync(
                    model.ProductId,
                    model.NewStock,
                    model.ChangeReason
                );

                TempData["SuccessMessage"] = $"Stock has been successfully updated for {model.ProductName}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View(model);
            }
        }


        // POST: Inventory/ToggleAvailability/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var product = await _dbContext.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Toggle availability
            product.IsAvailable = !(product.IsAvailable ?? true);

            _dbContext.Update(product);
            await _dbContext.SaveChangesAsync();

            string status = product.IsAvailable == true ? "available" : "unavailable";
            TempData["SuccessMessage"] = $"Product '{product.ProductName}' is now {status}.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Inventory/ConfirmDelete/5
        public async Task<IActionResult> ConfirmDelete(int id)
        {
            var product = await _dbContext.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var model = new DeleteConfirmationViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName
            };

            return View("DeleteConfirmation", model);
        }

        // POST: Inventory/DeleteProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(DeleteConfirmationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("DeleteConfirmation", model);
            }

            try
            {
                // Verify admin password
                var currentAdminId = int.Parse(User.FindFirst("AdminId").Value);
                var admin = await _dbContext.Admins.FindAsync(currentAdminId);

                if (admin == null)
                {
                    ModelState.AddModelError(string.Empty, "Unable to verify admin account.");
                    return View("DeleteConfirmation", model);
                }

                // Verify password
                var passwordHasher = new PasswordHasher<Admin>();
                var passwordVerificationResult = passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, model.Password);

                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("Password", "Incorrect password. Please try again.");
                    return View("DeleteConfirmation", model);
                }

                // Find the product
                var product = await _dbContext.Products.FindAsync(model.ProductId);
                if (product == null)
                {
                    return NotFound();
                }

                // Delete custom image file if exists
                if (!string.IsNullOrEmpty(product.ImagePath) &&
                    product.ImagePath.Contains("/custom/") &&
                    !product.ImagePath.StartsWith("/images/products/slim_container") &&
                    !product.ImagePath.StartsWith("/images/products/round_container") &&
                    !product.ImagePath.StartsWith("/images/products/refill"))
                {
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, product.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                // Check if there are any associated inventory logs
                var hasLogs = await _dbContext.InventoryLogs.AnyAsync(l => l.ProductId == model.ProductId);

                // If there are inventory logs, delete them first
                if (hasLogs)
                {
                    var logs = await _dbContext.InventoryLogs.Where(l => l.ProductId == model.ProductId).ToListAsync();
                    _dbContext.InventoryLogs.RemoveRange(logs);
                }

                // Check if the product is used in any order items
                var isUsedInOrders = await _dbContext.OrderItems.AnyAsync(o => o.ProductId == model.ProductId);
                if (isUsedInOrders)
                {
                    TempData["ErrorMessage"] = $"Cannot delete '{product.ProductName}' because it is associated with one or more orders.";
                    return RedirectToAction(nameof(Index));
                }

                // Delete the product
                _dbContext.Products.Remove(product);
                await _dbContext.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Product '{product.ProductName}' has been deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the product: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to check if product exists
        private bool ProductExists(int id)
        {
            return _dbContext.Products.Any(e => e.ProductId == id);
        }
    }
}