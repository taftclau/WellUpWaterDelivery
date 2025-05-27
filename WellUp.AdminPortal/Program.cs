//Program.cs
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WellUp.Core.Data;  // For accessing your WellUpDbContext
using WellUp.Core.Database;  // For accessing your model classes

namespace WellUp.AdminPortal
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register the DbContext
            builder.Services.AddDbContext<WellUpDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
          
            builder.Services.AddScoped<WellUp.Core.Utilities.ProductRelationshipManager>();
            // Program.cs
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                    // Only use Always in production; in development, use None or SameAsRequest
                    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                        ? CookieSecurePolicy.None
                        : CookieSecurePolicy.Always;
                });

            builder.Services.AddAuthorization(options =>
            {
                // Update this to match your controller attribute and login claim
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");

            // Instead of directly awaiting in Program.cs, you can use this pattern:
            if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                // Create a scope to get the service provider
                using (var scope = app.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        // Call the seeder without await, but use GetAwaiter().GetResult()
                        WellUp.Core.Seeds.AdminSeeder.SeedAdminUserAsync(services)
                            .GetAwaiter()
                            .GetResult();
                    }
                    catch (Exception ex)
                    {
                        var logger = services.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "An error occurred while seeding the database.");
                    }
                }
            }

            app.Run();
        }
    }
}