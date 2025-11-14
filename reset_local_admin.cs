// Script to reset admin password for LOCAL SQLite database
// Run: dotnet script reset_local_admin.cs

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

// Use SQLite for local database
var connectionString = "Data Source=app.db";

services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

var provider = services.BuildServiceProvider();
var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();

Console.WriteLine("🔧 Resetting admin user password for LOCAL database...");

// Create Admin role if it doesn't exist
if (!await roleManager.RoleExistsAsync("Admin"))
{
    await roleManager.CreateAsync(new IdentityRole("Admin"));
    Console.WriteLine("✅ Created Admin role");
}

const string adminEmail = "admin@sportadmin.se";
const string adminPassword = "vårloggaärgrön";

var adminUser = await userManager.FindByEmailAsync(adminEmail);

if (adminUser == null)
{
    Console.WriteLine("❌ Admin user not found! Creating...");
    adminUser = new ApplicationUser
    {
        UserName = adminEmail,
        Email = adminEmail,
        EmailConfirmed = true
    };
    var result = await userManager.CreateAsync(adminUser, adminPassword);
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
        Console.WriteLine($"✅ Created admin user: {adminEmail}");
    }
    else
    {
        Console.WriteLine($"❌ Failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        return;
    }
}
else
{
    Console.WriteLine($"✅ Admin user found: {adminEmail}");
    var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
    var resetResult = await userManager.ResetPasswordAsync(adminUser, token, adminPassword);
    if (resetResult.Succeeded)
    {
        Console.WriteLine($"✅ Password reset successfully for: {adminEmail}");
    }
    else
    {
        Console.WriteLine($"❌ Password reset failed: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
        return;
    }
    
    // Ensure Admin role
    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
        Console.WriteLine($"✅ Added Admin role to user");
    }
}

Console.WriteLine($"\n📋 Login credentials for LOCAL database:");
Console.WriteLine($"   Email: {adminEmail}");
Console.WriteLine($"   Password: {adminPassword}");
Console.WriteLine($"\n✅ Done! You can now log in to your local app.");




