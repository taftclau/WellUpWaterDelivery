using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WellUp.Core.Data;
using WellUp.CustomerPortal.Models;
using WellUp.CustomerPortal.Models.ViewModels;

namespace WellUp.CustomerPortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly WellUpDbContext _context;

        public HomeController(ILogger<HomeController> logger, WellUpDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel();

            try
            {
                // Debug line to check if we can retrieve any products at all
                var anyProducts = await _context.Products.AnyAsync();
                _logger.LogInformation($"Database contains products: {anyProducts}");

                // Get featured products with more lenient criteria
                // Modify query to handle nullable IsAvailable field better
                viewModel.FeaturedProducts = await _context.Products
                    .Where(p => p.IsAvailable == true || p.IsAvailable == null) // Include products where IsAvailable is true OR null
                    .OrderBy(p => Guid.NewGuid()) // Random order for featured products
                    .Take(4) // Limit to 4 featured products
                    .Select(p => new ProductViewModel
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        Description = p.Description ?? "",
                        ImagePath = p.ImagePath ?? "/images/products/default.png",
                        ContainerType = p.ContainerType,
                        IncludesRefill = p.IncludesRefill,
                        IsAvailable = p.IsAvailable ?? true
                    })
                    .ToListAsync();

                // Log how many products were retrieved
                _logger.LogInformation($"Retrieved {viewModel.FeaturedProducts.Count} featured products");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching featured products");
                // In case of error, continue with empty featured products
            }

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }
        public IActionResult Terms()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}