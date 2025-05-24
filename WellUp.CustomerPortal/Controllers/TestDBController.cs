// Create this file in both projects
// For admin: Controllers/TestDbController.cs
// For customer: Controllers/TestDbController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WellUp.Core.Data;

namespace WellUp.CustomerPortal.Controllers  // Or  for the other project
{
    public class TestDbController : Controller
    {
        private readonly WellUpDbContext _context;

        public TestDbController(WellUpDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Test basic connection (will throw if can't connect)
            bool canConnect = _context.Database.CanConnect();

            // Get a count from a table (replace with an actual table from your db)
            // For example if you have a Customers table:
            int customerCount = 0;
            try
            {
                customerCount = _context.Customers.Count();
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
            }

            ViewBag.CanConnect = canConnect;
            ViewBag.CustomerCount = customerCount;
            ViewBag.DbName = _context.Database.GetDbConnection().Database;
            ViewBag.ServerName = _context.Database.GetDbConnection().DataSource;

            return View();
        }
    }
}