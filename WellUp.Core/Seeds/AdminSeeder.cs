//Seeds/AdminSeeder.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using WellUp.Core.Data;
using WellUp.Core.Database;
using Microsoft.Extensions.Logging;

namespace WellUp.Core.Seeds
{
    public static class AdminSeeder
    {
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<WellUpDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<object>>();

                try
                {
                    logger.LogInformation("Checking for admin users");

                    // Check if admin exists
                    if (!await dbContext.Admins.AnyAsync())
                    {
                        logger.LogInformation("No admin users found. Creating default admin user.");

                        // Create default admin
                        var admin = new Admin
                        {
                            Email = "admin@wellup.com",
                            AdminName = "System Administrator",
                            DateModified = DateTime.Now
                        };

                        // Hash the password
                        var passwordHasher = new PasswordHasher<Admin>();
                        admin.PasswordHash = passwordHasher.HashPassword(admin, "Admin123!");

                        dbContext.Admins.Add(admin);
                        await dbContext.SaveChangesAsync();

                        logger.LogInformation("Default admin user created successfully.");
                    }
                    else
                    {
                        logger.LogInformation("Admin users exist, skipping seeding.");
                    }

                    // Seed default product data if needed
                    if (!await dbContext.Products.AnyAsync())
                    {
                        logger.LogInformation("No products found. Creating default product data.");

                        var products = new[]
                        {
                            new Product
                            {
                                ProductName = "5-Gallon Round Water Container",
                                Description = "Standard 5-gallon round water container for home or office",
                                ContainerType = "round",
                                IncludesRefill = true,
                                Price = 120.00m,
                                StockQuantity = 50,
                                LowStockThreshold = 10,
                                IsAvailable = true
                            },
                            new Product
                            {
                                ProductName = "Slim Water Container",
                                Description = "Space-saving slim design water container",
                                ContainerType = "slim",
                                IncludesRefill = true,
                                Price = 150.00m,
                                StockQuantity = 35,
                                LowStockThreshold = 10,
                                IsAvailable = true
                            },
                            new Product
                            {
                                ProductName = "Water Dispenser - Hot & Cold",
                                Description = "Dual temperature water dispenser unit",
                                ContainerType = null,
                                IncludesRefill = false,
                                Price = 2500.00m,
                                StockQuantity = 8,
                                LowStockThreshold = 5,
                                IsAvailable = true
                            },
                            new Product
                            {
                                ProductName = "Water Dispenser - Bottom Load",
                                Description = "Bottom loading dispenser for easier container handling",
                                ContainerType = null,
                                IncludesRefill = false,
                                Price = 3200.00m,
                                StockQuantity = 4,
                                LowStockThreshold = 5,
                                IsAvailable = true
                            },
                            new Product
                            {
                                ProductName = "Water Filter Replacement",
                                Description = "Replacement filter for WellUp water dispensers",
                                ContainerType = null,
                                IncludesRefill = false,
                                Price = 450.00m,
                                StockQuantity = 15,
                                LowStockThreshold = 10,
                                IsAvailable = true
                            },
                            new Product
                            {
                                ProductName = "Drinking Cups (Pack of 50)",
                                Description = "Disposable cups for water dispensers",
                                ContainerType = null,
                                IncludesRefill = false,
                                Price = 75.00m,
                                StockQuantity = 3,
                                LowStockThreshold = 10,
                                IsAvailable = true
                            }
                        };

                        dbContext.Products.AddRange(products);
                        await dbContext.SaveChangesAsync();

                        logger.LogInformation("Default product data created successfully.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }
        }
    }
}