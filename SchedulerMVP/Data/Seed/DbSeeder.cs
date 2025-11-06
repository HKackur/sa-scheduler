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
        
        // Seed henrik user (if not exists)
        await SeedHenrikUserAsync();

        // Note: Demo data seeding removed - users create their own data from scratch
        // No demo Places, Groups, or ScheduleTemplates are seeded
        _logger.LogInformation("Seed completed - admin and henrik users created.");
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

        const string adminPassword = "vårloggaärgrön";
        
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(adminUser, adminPassword);
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
            // User exists - only ensure they're in Admin role, don't reset password
            if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await _userManager.AddToRoleAsync(adminUser, "Admin");
                _logger.LogInformation("Added admin user to Admin role.");
            }
            else
            {
                _logger.LogInformation("Admin user already exists with correct role - password not changed.");
            }
        }
    }
    
    private async Task SeedHenrikUserAsync()
    {
        const string henrikEmail = "henrik.kackur@sportadmin.se";
        var henrikUser = await _userManager.FindByEmailAsync(henrikEmail);
        
        // Use same password as admin for now - can be changed later
        const string henrikPassword = "vårloggaärgrön";
        
        if (henrikUser == null)
        {
            henrikUser = new ApplicationUser
            {
                UserName = henrikEmail,
                Email = henrikEmail,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(henrikUser, henrikPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("Created henrik user: {Email}", henrikEmail);
            }
            else
            {
                _logger.LogError("Failed to create henrik user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // User exists - don't reset password, let user keep their own password
            _logger.LogInformation("Henrik user already exists - password not changed.");
        }
    }
}


