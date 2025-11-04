#!/bin/bash
# Fix admin user password in Azure
echo "🔧 Resetting admin user password..."

# Use connection string from Azure
CONN_STR="Host=aws-1-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.anebyqfrzsuqwrbncwxt;Password=bunch-hiccups-misery-extreme;SSL Mode=Require;Trust Server Certificate=true"

# Run dotnet script to reset password
dotnet script << 'DOTNETSCRIPT'
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
    ?? "Host=aws-1-eu-west-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.anebyqfrzsuqwrbncwxt;Password=bunch-hiccups-misery-extreme;SSL Mode=Require;Trust Server Certificate=true";

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
    }
}
else
{
    Console.WriteLine($"✅ Admin user exists: {adminEmail}");
    var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
    var resetResult = await userManager.ResetPasswordAsync(adminUser, token, adminPassword);
    if (resetResult.Succeeded)
    {
        Console.WriteLine($"✅ Password reset successfully for: {adminEmail}");
    }
    else
    {
        Console.WriteLine($"❌ Password reset failed: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
    }
    
    // Ensure Admin role
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }
    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
        Console.WriteLine($"✅ Added Admin role to user");
    }
}

Console.WriteLine($"\n📋 Login credentials:");
Console.WriteLine($"   Email: {adminEmail}");
Console.WriteLine($"   Password: {adminPassword}");
DOTNETSCRIPT
