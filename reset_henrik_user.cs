// Script to reset henrik.kackur@sportadmin.se password for LOCAL SQLite database
// Run: dotnet script reset_henrik_user.cs

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

Console.WriteLine("🔧 Resetting henrik.kackur@sportadmin.se password for LOCAL database...");

const string henrikEmail = "henrik.kackur@sportadmin.se";
const string henrikPassword = "vårloggaärgrön";

var henrikUser = await userManager.FindByEmailAsync(henrikEmail);

if (henrikUser == null)
{
    Console.WriteLine("❌ User not found! Creating...");
    henrikUser = new ApplicationUser
    {
        UserName = henrikEmail,
        Email = henrikEmail,
        EmailConfirmed = true
    };
    var result = await userManager.CreateAsync(henrikUser, henrikPassword);
    if (result.Succeeded)
    {
        Console.WriteLine($"✅ Created user: {henrikEmail}");
    }
    else
    {
        Console.WriteLine($"❌ Failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        return;
    }
}
else
{
    Console.WriteLine($"✅ User found: {henrikEmail}");
    var token = await userManager.GeneratePasswordResetTokenAsync(henrikUser);
    var resetResult = await userManager.ResetPasswordAsync(henrikUser, token, henrikPassword);
    if (resetResult.Succeeded)
    {
        Console.WriteLine($"✅ Password reset successfully for: {henrikEmail}");
    }
    else
    {
        Console.WriteLine($"❌ Password reset failed: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
        return;
    }
}

Console.WriteLine($"\n📋 Login credentials for LOCAL database:");
Console.WriteLine($"   Email: {henrikEmail}");
Console.WriteLine($"   Password: {henrikPassword}");
Console.WriteLine($"\n✅ Done! You can now log in to your local app.");




