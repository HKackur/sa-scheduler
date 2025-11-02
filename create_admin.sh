#!/bin/bash
set -e

echo "🔧 Creating admin user in Supabase..."

cd SchedulerMVP

# Set connection string for Supabase
export ConnectionStrings__DefaultConnection="Host=db.anebyqfrzsuqwrbncwxt.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=bunch-hiccups-misery-extreme;SSL Mode=Require;"

# Build the project first
echo "📦 Building project..."
dotnet build -c Release --no-restore > /dev/null 2>&1 || dotnet build -c Release

# Run a simple C# program to create the user
dotnet run --no-build --project SchedulerMVP.csproj << 'EOF'
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

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
    adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
    var result = await userManager.CreateAsync(adminUser, adminPassword);
    if (result.Succeeded)
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
        Console.WriteLine($"✅ Created admin user: {adminEmail}");
    }
    else
    {
        Console.WriteLine($"❌ Failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
    }
}
else
{
    var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
    var resetResult = await userManager.ResetPasswordAsync(adminUser, token, adminPassword);
    if (resetResult.Succeeded)
    {
        Console.WriteLine($"✅ Password updated for: {adminEmail}");
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
EOF

