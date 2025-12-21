#!/bin/bash
set -e

echo "🔧 Creating henrik.kackur@sportadmin.se user for LOCAL database..."

cd SchedulerMVP

# Use SQLite for local database
export ConnectionStrings__DefaultConnection="Data Source=app.db"

# Build the project first
echo "📦 Building project..."
dotnet build -c Debug --no-restore > /dev/null 2>&1 || dotnet build -c Debug

# Create a temporary C# file to create the user
cat > /tmp/create_henrik_user.cs << 'EOF'
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;

var services = new ServiceCollection();
services.AddLogging(b => b.AddConsole());

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
    ?? "Data Source=app.db";

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
        Environment.Exit(1);
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
        Environment.Exit(1);
    }
}

Console.WriteLine($"\n📋 Login credentials for LOCAL database:");
Console.WriteLine($"   Email: {henrikEmail}");
Console.WriteLine($"   Password: {henrikPassword}");
Console.WriteLine($"\n✅ Done! You can now log in to your local app.");
EOF

# Run the script using dotnet-script if available, otherwise use a workaround
if command -v dotnet-script >/dev/null 2>&1; then
    dotnet script /tmp/create_henrik_user.cs
else
    # Use a simpler approach - just start the app and it will create the user via DbSeeder
    echo "ℹ️  Note: henrik.kackur@sportadmin.se will be created automatically when you start the app."
    echo "   The app uses DbSeeder to create this user on startup."
    echo "   Just start the app with: dotnet run --project SchedulerMVP"
fi

rm -f /tmp/create_henrik_user.cs



