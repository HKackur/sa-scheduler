using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data.Entities;

namespace SchedulerMVP.Data.Seed;

public class DbSeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<DbSeeder> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DbSeeder(AppDbContext db, ILogger<DbSeeder> logger, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _logger = logger;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        // Seed admin user and role (if not exists)
        await SeedAdminUserAsync();

        // Note: Demo data seeding removed - users create their own data from scratch
        // No demo Places, Groups, or ScheduleTemplates are seeded
        _logger.LogInformation("Seed completed - only admin user created.");
    }

    private async Task SeedAdminUserAsync()
    {
        // Create Admin role if it doesn't exist
        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Admin"));
            _logger.LogInformation("Created Admin role.");
        }

        // Create admin user if it doesn't exist
        const string adminEmail = "admin@sportadmin.se";
        var adminUser = await _userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(adminUser, "Test1234!");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation("Created admin user: {Email}", adminEmail);
            }
            else
            {
                _logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            _logger.LogInformation("Admin user already exists.");
        }
    }
}


