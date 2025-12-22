// Script to create admin user for henrik.kackur@gmail.com in LOCAL SQLite database
// Run: dotnet script create_henrik_admin.cs

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

Console.WriteLine("🔧 Creating admin user for henrik.kackur@gmail.com in LOCAL database...");

// Create Admin role if it doesn't exist
if (!await roleManager.RoleExistsAsync("Admin"))
{
    await roleManager.CreateAsync(new IdentityRole("Admin"));
    Console.WriteLine("✅ Created Admin role");
}
else
{
    Console.WriteLine("ℹ️  Admin role already exists");
}

const string henrikEmail = "henrik.kackur@gmail.com";
const string henrikPassword = "vårloggaärgrön";

var henrikUser = await userManager.FindByEmailAsync(henrikEmail);

if (henrikUser == null)
{
    Console.WriteLine($"Creating new user: {henrikEmail}");
    henrikUser = new ApplicationUser
    {
        UserName = henrikEmail,
        Email = henrikEmail,
        EmailConfirmed = true
    };
    var result = await userManager.CreateAsync(henrikUser, henrikPassword);
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(henrikUser, "Admin");
        Console.WriteLine($"✅ Created admin user: {henrikEmail}");
    }
    else
    {
        Console.WriteLine($"❌ Failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        Environment.Exit(1);
    }
}
else
{
    Console.WriteLine($"✅ User already exists: {henrikEmail}");
    
    // Ensure user is in Admin role
    if (!await userManager.IsInRoleAsync(henrikUser, "Admin"))
    {
        await userManager.AddToRoleAsync(henrikUser, "Admin");
        Console.WriteLine("✅ Added user to Admin role");
    }
    else
    {
        Console.WriteLine("ℹ️  User already in Admin role");
    }
    
    // Reset password
    var token = await userManager.GeneratePasswordResetTokenAsync(henrikUser);
    var resetResult = await userManager.ResetPasswordAsync(henrikUser, token, henrikPassword);
    if (resetResult.Succeeded)
    {
        Console.WriteLine($"✅ Password reset successfully for: {henrikEmail}");
    }
    else
    {
        Console.WriteLine($"❌ Password reset failed: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
        Environment.Exit(1);
    }
}

Console.WriteLine($"\n📋 Login credentials for LOCAL database:");
Console.WriteLine($"   Email: {henrikEmail}");
Console.WriteLine($"   Password: {henrikPassword}");
Console.WriteLine($"\n✅ Done! You can now log in to your local app as admin.");
