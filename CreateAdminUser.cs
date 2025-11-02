using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
    ?? "Host=db.anebyqfrzsuqwrbncwxt.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=bunch-hiccups-misery-extreme;SSL Mode=Require;";

services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

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

Console.WriteLine("🔧 Creating admin user...");

// Create Admin role
if (!await roleManager.RoleExistsAsync("Admin"))
{
    await roleManager.CreateAsync(new IdentityRole("Admin"));
    Console.WriteLine("✅ Created Admin role");
}
else
{
    Console.WriteLine("ℹ️  Admin role already exists");
}

// Create admin user
const string adminEmail = "admin@sportadmin.se";
const string adminPassword = "vårloggaärgrön";

var adminUser = await userManager.FindByEmailAsync(adminEmail);

if (adminUser == null)
{
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
        Console.WriteLine($"❌ Failed to create admin user:");
        foreach (var error in result.Errors)
        {
            Console.WriteLine($"   - {error.Description}");
        }
        Environment.Exit(1);
    }
}
else
{
    // Update password if user exists
    var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
    var resetResult = await userManager.ResetPasswordAsync(adminUser, token, adminPassword);
    if (resetResult.Succeeded)
    {
        Console.WriteLine($"✅ Admin user already exists - password updated: {adminEmail}");
        
        // Ensure user is in Admin role
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            Console.WriteLine("✅ Added admin user to Admin role");
        }
        else
        {
            Console.WriteLine("ℹ️  User already in Admin role");
        }
    }
    else
    {
        Console.WriteLine($"❌ Failed to update password:");
        foreach (var error in resetResult.Errors)
        {
            Console.WriteLine($"   - {error.Description}");
        }
        Environment.Exit(1);
    }
}

Console.WriteLine("\n🎉 Done! You can now log in with:");
Console.WriteLine($"   Email: {adminEmail}");
Console.WriteLine($"   Password: {adminPassword}");

