using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;
using SchedulerMVP.Data.Seed;
using SchedulerMVP.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using Npgsql;
using System.Net.NetworkInformation;
using System.Globalization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data.Common;
using System.Collections.Generic;

// Fix Azure culture issue - set default culture to avoid "__10" invalid culture error
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

// Check if we should create test user instead of running the web app
if (args.Length > 0 && args[0] == "create-test-user")
{
    var services = new ServiceCollection();
    services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

    // Use SQLite for local database
    var testUserConnectionString = "Data Source=app.db";

    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(testUserConnectionString));

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

    Console.WriteLine("🔧 Creating test user for LOCAL database...");

    const string testEmail = "test@kackur.se";
    const string testPassword = "test1234";

    var testUser = await userManager.FindByEmailAsync(testEmail);

    if (testUser == null)
    {
        Console.WriteLine($"Creating new user: {testEmail}");
        testUser = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(testUser, testPassword);
        if (result.Succeeded)
        {
            Console.WriteLine($"✅ Created test user: {testEmail}");
        }
        else
        {
            Console.WriteLine($"❌ Failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            Environment.Exit(1);
        }
    }
    else
    {
        Console.WriteLine($"✅ User already exists: {testEmail}");
        var token = await userManager.GeneratePasswordResetTokenAsync(testUser);
        var resetResult = await userManager.ResetPasswordAsync(testUser, token, testPassword);
        if (resetResult.Succeeded)
        {
            Console.WriteLine($"✅ Password reset successfully for: {testEmail}");
        }
        else
        {
            Console.WriteLine($"❌ Password reset failed: {string.Join(", ", resetResult.Errors.Select(e => e.Description))}");
            Environment.Exit(1);
        }
    }

    Console.WriteLine($"\n📋 Login credentials for LOCAL database:");
    Console.WriteLine($"   Email: {testEmail}");
    Console.WriteLine($"   Password: {testPassword}");
    Console.WriteLine($"\n✅ Done! You can now log in to your local app.");
    
    Environment.Exit(0);
}

var builder = WebApplication.CreateBuilder(args);

// Force IPv4 for Supabase connections (Azure Basic B1 doesn't support IPv6)
// Configure Npgsql to prefer IPv4
var supabaseHost = "db.anebyqfrzsuqwrbncwxt.supabase.co";

// DataProtection: Persist keys across app restarts for authentication cookies
// Azure App Service: Uses persistent home directory
if (!builder.Environment.IsDevelopment())
{
    // Azure App Service: Use persistent home directory (survives restarts and deployments)
    // Linux App Service exposes HOME=/home; Windows uses D:\\home
    string? azureHome = Environment.GetEnvironmentVariable("HOME");
    string azureKeysPath;
    if (!string.IsNullOrEmpty(azureHome))
    {
        azureKeysPath = Path.Combine(azureHome, "data", "ProtectionKeys");
    }
    else
    {
        var homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE") ?? "D:";
        var homePath = Environment.GetEnvironmentVariable("HOMEPATH") ?? @"\home";
        azureKeysPath = Path.Combine(homeDrive, homePath.TrimStart('\\', '/'), "data", "ProtectionKeys");
    }
    
    if (!Directory.Exists(azureKeysPath))
    {
        Directory.CreateDirectory(azureKeysPath);
    }
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(azureKeysPath))
        .SetApplicationName("SchedulerMVP");
}

// Port configuration: Azure App Service sets PORT environment variable - app MUST listen on that port
// CRITICAL: Azure App Service has issues with HTTP/2 - use HTTP/1.1 only for Azure
// For Azure, we MUST disable HTTP/2 completely to avoid ERR_HTTP2_PROTOCOL_ERROR
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port) && int.TryParse(port, out var portNumber))
{
    // Azure App Service: Use HTTP/1.1 ONLY - Azure proxy has HTTP/2 issues
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(portNumber, listenOptions =>
        {
            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
        });
        // Also set default to HTTP/1.1 for any other endpoints
        options.ConfigureEndpointDefaults(lo => lo.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
    });
}
else
{
    // Local dev: Use default 8080 with HTTP/1.1 and HTTP/2
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(8080);
        options.ConfigureEndpointDefaults(lo => lo.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
    });
}

// Add services to the container.
builder.Services.AddRazorPages();

// Add memory cache for performance optimization (caching Groups, Places, Areas)
builder.Services.AddMemoryCache();
// CRITICAL: Configure SignalR BEFORE AddServerSideBlazor for Blazor Server
// This ensures proper connection handling behind proxy
builder.Services.AddSignalR(options =>
{
    // Optimized timeouts for better performance and handling of inactivity
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60); // Increased from 30s to handle longer inactivity periods
    options.KeepAliveInterval = TimeSpan.FromSeconds(15); // Increased from 10s to reduce overhead while maintaining connection health
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.EnableDetailedErrors = builder.Environment.IsDevelopment(); // Enable in development for debugging
    options.MaximumReceiveMessageSize = 64 * 1024; // Increased to 64KB for larger render batches
});

builder.Services.AddServerSideBlazor(options =>
{
    // Enable detailed errors to debug Azure issues
    options.DetailedErrors = true; // Enable in production to see what's causing crashes
    options.DisconnectedCircuitMaxRetained = 100;
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(5); // Increased from 3 minutes to allow more time for reconnection
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    options.MaxBufferedUnacknowledgedRenderBatches = 30; // Increased from 20 for better performance
});

// Get connection string - Azure App Service exposes connection strings in multiple ways
// Priority order (Azure App Service specific):
// 1. App Setting with double underscore (ConnectionStrings__DefaultConnection) - Most reliable in Azure
// 2. Connection String type (GetConnectionString) - May not work correctly in all Azure scenarios
// 3. App Setting with colon (ConnectionStrings:DefaultConnection)
// 4. Direct app setting (DefaultConnection)
var connectionString = builder.Configuration["ConnectionStrings__DefaultConnection"];
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];
}
if (string.IsNullOrEmpty(connectionString))
{
    // Try direct app setting
    connectionString = builder.Configuration["DefaultConnection"];
}

// Connection string should be set correctly in Azure App Service
// For Session pooler: use aws-1-eu-west-1.pooler.supabase.com:5432
// For Transaction pooler: use aws-1-eu-west-1.pooler.supabase.com:6543
// Don't modify connection string here - use exact value from Azure Portal

// Log connection string status (without exposing password)
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("[ERROR] Connection string is null or empty! Will use SQLite fallback.");
    Console.WriteLine("[DEBUG] Available config keys:");
    foreach (var key in builder.Configuration.AsEnumerable())
    {
        if (key.Key.Contains("Connection", StringComparison.OrdinalIgnoreCase) || 
            key.Key.Contains("Database", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[DEBUG]   {key.Key} = {(key.Value?.Length > 50 ? key.Value.Substring(0, 50) + "..." : key.Value)}");
        }
    }
}
else
{
    var host = connectionString.Split(';').FirstOrDefault(s => s.StartsWith("Host="))?.Replace("Host=", "") ?? "unknown";
    var connectionPort = connectionString.Split(';').FirstOrDefault(s => s.StartsWith("Port="))?.Replace("Port=", "") ?? "unknown";
    Console.WriteLine($"[INFO] Connection string found, Host: {host}, Port: {connectionPort}");
    Console.WriteLine($"[INFO] Will use PostgreSQL (Supabase) database");
}

// Add ApplicationDbContext for Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly("SchedulerMVP");
            npgsql.CommandTimeout(120); // 120 second timeout for Azure
            // Connection pooling is configured via connection string parameters:
            // MaxPoolSize=10;MinPoolSize=2 (if needed, add to connection string)
        });
    }
    else
    {
        options.UseSqlite("Data Source=app.db", sqlite =>
            sqlite.MigrationsAssembly("SchedulerMVP"));
    }
}, ServiceLifetime.Scoped);

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// CRITICAL: Use ServerAuthenticationStateProvider for Blazor Server
// This is REQUIRED for Blazor Server to preserve auth state during circuit reconnects
// Without this, users get logged out when SignalR reconnects
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

// Persist login for 7 days (sliding)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
    // Required for HTTPS/proxy environments (Azure App Service)
    options.Cookie.SameSite = SameSiteMode.Lax; // Use Lax for better compatibility
    // CRITICAL: Use Always in production (HTTPS)
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    // CRITICAL: MaxAge ensures cookie persists across browser restarts
    options.Cookie.MaxAge = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".AspNetCore.Identity.Application";
    options.Cookie.IsEssential = true; // Required for authentication
    options.Cookie.Path = "/"; // Ensure cookie is sent for all paths including /_blazor
});

// Add AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly("SchedulerMVP");
            npgsql.CommandTimeout(120); // 120 second timeout for Azure
            // Connection pooling is configured via connection string parameters:
            // MaxPoolSize=10;MinPoolSize=2 (if needed, add to connection string)
        });
    }
    else
    {
        options.UseSqlite("Data Source=app.db", sqlite =>
            sqlite.MigrationsAssembly("SchedulerMVP"));
    }
}, ServiceLifetime.Scoped);

builder.Services.AddHttpContextAccessor();

// Add DbContextFactory for thread-safe DbContext access in Blazor Server
// CRITICAL: Must use AddDbContextFactory (not AddDbContext) with proper lifetime
builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly("SchedulerMVP");
            npgsql.CommandTimeout(120); // 120 second timeout for Azure
            // Connection pooling is configured via connection string parameters:
            // MaxPoolSize=10;MinPoolSize=2 (if needed, add to connection string)
        });
    }
    else
    {
        options.UseSqlite("Data Source=app.db", sqlite =>
            sqlite.MigrationsAssembly("SchedulerMVP"));
    }
}, ServiceLifetime.Scoped); // CRITICAL: Must be Scoped, not Singleton

// Add DbContextFactory for ApplicationDbContext (Identity)
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly("SchedulerMVP");
            npgsql.CommandTimeout(120); // 120 second timeout for Azure
            // Connection pooling is configured via connection string parameters:
            // MaxPoolSize=10;MinPoolSize=2 (if needed, add to connection string)
        });
    }
    else
    {
        options.UseSqlite("Data Source=app.db", sqlite =>
            sqlite.MigrationsAssembly("SchedulerMVP"));
    }
}, ServiceLifetime.Scoped); // CRITICAL: Must be Scoped, not Singleton

// Add services
builder.Services.AddScoped<IConflictService, ConflictService>();
builder.Services.AddScoped<IScheduleTemplateService, ScheduleTemplateService>();
builder.Services.AddScoped<IPlaceService, PlaceService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<ICalendarBookingService, CalendarBookingService>();
builder.Services.AddScoped<IModalService, ModalService>();
builder.Services.AddScoped<BookingDialogService>();
builder.Services.AddScoped<UIState>();
builder.Services.AddScoped<DbSeeder>();
builder.Services.AddScoped<UserContextService>();
builder.Services.AddScoped<IGroupTypeService, GroupTypeService>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddScoped<RoleManager<IdentityRole>>();
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Respect proxy headers: Azure App Service
// CRITICAL: Include XForwardedHost for SignalR base path detection
// Azure App Service uses X-Forwarded-* headers
// MUST be called before UseStaticFiles and other middleware
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    // Clear known networks/proxies to allow Azure App Service proxy
    KnownNetworks = { },
    KnownProxies = { },
    // Azure App Service requirement: require at least one header
    RequireHeaderSymmetry = false
});

// Configure the HTTP request pipeline.
// Always use detailed error page in production for now to debug issues
app.UseExceptionHandler("/Error");

// HSTS: Only enable if we're sure we're on HTTPS
// UseForwardedHeaders should have already set the scheme correctly
app.UseHsts();

app.UseStaticFiles();
app.UseRouting();

// --- Debug endpoints (must be BEFORE authentication/authorization) ---
app.MapGet("/debug/test-password", async (HttpContext context, string email, string password) =>
{
    try
    {
        var scope = context.RequestServices.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // List all users to help debug
            var allUsers = await userManager.Users.Select(u => new { u.Email, u.UserName, u.Id }).ToListAsync();
            scope.Dispose();
            return Results.Ok(new 
            { 
                found = false, 
                message = $"User with email {email} not found",
                totalUsers = allUsers.Count,
                users = allUsers
            });
        }
        
        var passwordCheck = await userManager.CheckPasswordAsync(user, password);
        var hasPassword = !string.IsNullOrEmpty(user.PasswordHash);
        var isInAdminRole = await userManager.IsInRoleAsync(user, "Admin");
        
        scope.Dispose();
        
        return Results.Ok(new 
        { 
            found = true,
            email = user.Email,
            userName = user.UserName,
            hasPassword,
            passwordCheck,
            emailConfirmed = user.EmailConfirmed,
            isInAdminRole
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error: {ex.Message}\n{ex.StackTrace}");
    }
});

app.MapGet("/debug/list-users", async (HttpContext context) =>
{
    try
    {
        var scope = context.RequestServices.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var users = await userManager.Users.Select(u => new 
        { 
            u.Email, 
            u.UserName, 
            u.Id,
            HasPassword = !string.IsNullOrEmpty(u.PasswordHash),
            u.EmailConfirmed
        }).ToListAsync();
        
        scope.Dispose();
        
        return Results.Ok(new 
        { 
            totalUsers = users.Count,
            users = users
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error: {ex.Message}\n{ex.StackTrace}");
    }
});

app.MapGet("/debug/reset-password", async (HttpContext context, string email, string newPassword) =>
{
    try
    {
        var scope = context.RequestServices.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            scope.Dispose();
            return Results.NotFound(new { message = $"User with email {email} not found" });
        }
        
        // Remove old password
        var removeResult = await userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            scope.Dispose();
            return Results.Problem($"Failed to remove old password: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}");
        }
        
        // Add new password
        var addResult = await userManager.AddPasswordAsync(user, newPassword);
        if (addResult.Succeeded)
        {
            scope.Dispose();
            return Results.Ok(new 
            { 
                success = true,
                message = $"Password reset successfully for {email}",
                email = user.Email
            });
        }
        else
        {
            scope.Dispose();
            return Results.Problem($"Failed to set new password: {string.Join(", ", addResult.Errors.Select(e => e.Description))}");
        }
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error: {ex.Message}\n{ex.StackTrace}");
    }
});

app.MapGet("/debug/users", async (HttpContext context) =>
{
    try
    {
        var scope = context.RequestServices.CreateScope();
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        
        // Get connection string
        var connStr = configuration.GetConnectionString("DefaultConnection") 
            ?? configuration["ConnectionStrings:DefaultConnection"]
            ?? configuration["ConnectionStrings__DefaultConnection"];
        
        logger.LogInformation("Connection string found: {Found}, Length: {Length}", !string.IsNullOrEmpty(connStr), connStr?.Length ?? 0);
        
        // Test DIRECT Npgsql connection (bypass EF Core)
        string directConnectionError = null;
        bool directConnectionSuccess = false;
        try
        {
            using var directConn = new Npgsql.NpgsqlConnection(connStr);
            await directConn.OpenAsync();
            using var cmd = new Npgsql.NpgsqlCommand("SELECT 1", directConn);
            await cmd.ExecuteScalarAsync();
            directConnectionSuccess = true;
            await directConn.CloseAsync();
        }
        catch (Exception ex)
        {
            directConnectionError = $"{ex.GetType().Name}: {ex.Message}\nInner: {ex.InnerException?.Message}";
            logger.LogError(ex, "Direct Npgsql connection failed");
        }
        
        // Try to connect to database via EF Core
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        string dbError = null;
        bool canConnect = false;
        try
        {
            await dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
            canConnect = true;
            logger.LogInformation("EF Core database connection successful");
        }
        catch (Exception ex)
        {
            canConnect = false;
            dbError = $"{ex.GetType().Name}: {ex.Message}\nInner: {ex.InnerException?.Message}";
            logger.LogError(ex, "EF Core database connection failed");
        }
        
        // Try to query users
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        string queryError = null;
        int userCount = 0;
        try
        {
            if (canConnect)
            {
                var users = await userManager.Users.Take(10).Select(u => new { u.Email, u.Id }).ToListAsync();
                userCount = users.Count;
            }
            else
            {
                queryError = "Cannot query - database connection failed";
            }
        }
        catch (Exception ex)
        {
            queryError = $"{ex.GetType().Name}: {ex.Message}\nInner: {ex.InnerException?.Message}";
            logger.LogError(ex, "User query failed");
        }
        
        scope.Dispose();
        
        return Results.Ok(new 
        { 
            userCount,
            connectionStringFound = !string.IsNullOrEmpty(connStr),
            connectionStringHost = connStr?.Split(';').FirstOrDefault(s => s.StartsWith("Host="))?.Replace("Host=", "") ?? "unknown",
            connectionStringPort = connStr?.Split(';').FirstOrDefault(s => s.StartsWith("Port="))?.Replace("Port=", "") ?? "unknown",
            connectionStringUsername = connStr?.Split(';').FirstOrDefault(s => s.StartsWith("Username="))?.Replace("Username=", "") ?? "unknown",
            directNpgsqlConnection = directConnectionSuccess ? "success" : $"failed: {directConnectionError}",
            efCoreConnection = canConnect ? "success" : $"failed: {dbError}",
            queryError
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error: {ex.Message}\n{ex.StackTrace}");
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Admin endpoint to fix database columns (temporary, remove after use)
app.MapGet("/admin/fix-db-columns", async (AppDbContext context, IServiceProvider services) =>
{
    try
    {
        await context.Database.OpenConnectionAsync();
        var scope = services.CreateScope();
        
        // Fix Groups table
        await context.Database.ExecuteSqlRawAsync(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'Groups' AND column_name = 'Source'
                ) THEN
                    ALTER TABLE ""Groups"" ADD COLUMN ""Source"" VARCHAR(50) NOT NULL DEFAULT 'Egen';
                END IF;
            END $$;
        ");
        
        await context.Database.ExecuteSqlRawAsync(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'Groups' AND column_name = 'DisplayColor'
                ) THEN
                    ALTER TABLE ""Groups"" ADD COLUMN ""DisplayColor"" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå';
                END IF;
            END $$;
        ");
        
        var updated = await context.Database.ExecuteSqlRawAsync(@"
            UPDATE ""Groups"" 
            SET ""Source"" = COALESCE(""Source"", 'Egen'),
                ""DisplayColor"" = COALESCE(""DisplayColor"", 'Ljusblå')
            WHERE ""Source"" IS NULL OR ""DisplayColor"" IS NULL
        ");
        
        // Fix GroupTypes table
        await context.Database.ExecuteSqlRawAsync(@"
            DO $$ 
            BEGIN 
                IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_name = 'GroupTypes' AND column_name = 'StandardDisplayColor'
                ) THEN
                    ALTER TABLE ""GroupTypes"" ADD COLUMN ""StandardDisplayColor"" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå';
                END IF;
            END $$;
        ");
        
        var updatedTypes = await context.Database.ExecuteSqlRawAsync(@"
            UPDATE ""GroupTypes"" 
            SET ""StandardDisplayColor"" = COALESCE(""StandardDisplayColor"", 'Ljusblå')
            WHERE ""StandardDisplayColor"" IS NULL
        ");
        
        // Fix UserId for groups without owner (attach to admin)
        const string adminEmail = "admin@sportadmin.se";
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser != null)
        {
            var adminId = adminUser.Id;
            var fixedUserId = await context.Database.ExecuteSqlRawAsync(@"
                UPDATE ""Groups"" 
                SET ""UserId"" = {0} 
                WHERE ""UserId"" IS NULL OR TRIM(""UserId"") = ''
            ", adminId);
            Console.WriteLine($"[FIX] Fixed UserId for {fixedUserId} groups");
        }
        
        // Check how many groups exist
        var groupCount = await context.Database.SqlQueryRaw<int>(@"SELECT COUNT(*) FROM ""Groups""").FirstOrDefaultAsync();
        var groupsWithUserId = await context.Database.SqlQueryRaw<int>(@"SELECT COUNT(*) FROM ""Groups"" WHERE ""UserId"" IS NOT NULL AND TRIM(""UserId"") != ''").FirstOrDefaultAsync();
        
        return Results.Ok(new { 
            success = true, 
            message = $"Fixed! Updated {updated} groups and {updatedTypes} group types.",
            totalGroups = groupCount,
            groupsWithUserId = groupsWithUserId
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error: {ex.Message}");
    }
});

// Configure Blazor Server SignalR hub
app.MapBlazorHub(options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets | 
                         Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(90);
});

app.MapFallbackToPage("/_Host");

// --- Run migrations and seeding in background (non-blocking) ---
// This allows the app to start accepting requests immediately
// Migrations/seeding will complete in the background
_ = Task.Run(async () =>
{
    // Wait a bit for the app to fully start
    await Task.Delay(TimeSpan.FromSeconds(2));
    
    var scope = app.Services.CreateScope();
    try
    {
        var identityContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        Console.WriteLine("[MIGRATION] Starting Identity database migration (background)...");
        logger.LogInformation("=== Starting Identity database migration (background) ===");
        
        try
        {
            var pendingMigrations = await identityContext.Database.GetPendingMigrationsAsync();
            Console.WriteLine($"[MIGRATION] Pending Identity migrations: {pendingMigrations.Count()}");
            logger.LogInformation("Pending Identity migrations: {Count}", pendingMigrations.Count());
            foreach (var migration in pendingMigrations)
            {
                Console.WriteLine($"[MIGRATION]   - {migration}");
                logger.LogInformation("  - {Migration}", migration);
            }
            
            await identityContext.Database.MigrateAsync();
            Console.WriteLine("[MIGRATION] Identity database migration completed");
            logger.LogInformation("=== Identity database migration completed ===");
            
            // ONBOARDING REMOVED - Drop OnboardingCompletedStep column if it exists
            try
            {
                var identityDbProvider = identityContext.Database.ProviderName ?? string.Empty;
                if (identityDbProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
                {
                    await identityContext.Database.ExecuteSqlRawAsync(@"
                        DO $$
                        BEGIN
                            IF EXISTS (
                                SELECT 1 FROM information_schema.columns 
                                WHERE table_name = 'AspNetUsers' 
                                AND column_name = 'OnboardingCompletedStep'
                            ) THEN
                                ALTER TABLE ""AspNetUsers"" 
                                DROP COLUMN ""OnboardingCompletedStep"";
                            END IF;
                        END $$;
                    ");
                    Console.WriteLine("[MIGRATION] OnboardingCompletedStep column removed if it existed");
                    logger.LogInformation("OnboardingCompletedStep column removed if it existed");
                }
            }
            catch (Exception dropEx)
            {
                Console.WriteLine($"[MIGRATION] Column drop check (non-critical): {dropEx.Message}");
                logger.LogInformation("Column drop check (non-critical): {Message}", dropEx.Message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MIGRATION] ERROR: Failed to migrate Identity database: {ex.Message}");
            logger.LogError(ex, "=== FAILED to migrate Identity database: {Message} ===", ex.Message);
        }
        
        // Ensure application database is up-to-date
        var provider = context.Database.ProviderName ?? string.Empty;
        Console.WriteLine($"[MIGRATION] Starting AppDbContext migration (provider: {provider})...");
        logger.LogInformation("=== Starting AppDbContext migration (provider: {Provider}) ===", provider);
        
        try
        {
            if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                Console.WriteLine($"[MIGRATION] Pending AppDbContext migrations: {pendingMigrations.Count()}");
                logger.LogInformation("Pending AppDbContext migrations: {Count}", pendingMigrations.Count());
                foreach (var migration in pendingMigrations)
                {
                    Console.WriteLine($"[MIGRATION]   - {migration}");
                    logger.LogInformation("  - {Migration}", migration);
                }
                
                await context.Database.MigrateAsync();
                Console.WriteLine("[MIGRATION] AppDbContext migration completed");
                logger.LogInformation("=== AppDbContext migration completed ===");
            }
            else
            {
                Console.WriteLine("[MIGRATION] Using EnsureCreated (not Npgsql provider)");
                logger.LogInformation("Using EnsureCreated (not Npgsql provider)");
                await context.Database.EnsureCreatedAsync();
                Console.WriteLine("[MIGRATION] AppDbContext EnsureCreated completed");
                logger.LogInformation("=== AppDbContext EnsureCreated completed ===");
                
                // Fix SourceTemplateId foreign key constraint for SQLite
                // SQLite doesn't support DROP CONSTRAINT, so we need to recreate the table
                // This is a one-time fix that ensures the constraint points to ScheduleTemplates (not BookingTemplates)
                try
                {
                    // Check if constraint needs fixing by checking the table schema
                    // We'll check if the constraint exists and points to the wrong table
                    var needsFix = false;
                    try
                    {
                        // Try to insert a test value with a ScheduleTemplate ID - if it fails with FK error,
                        // the constraint might be pointing to BookingTemplates instead
                        // But we can't easily check this, so we'll just run the fix once
                        // Use a marker table to track if we've already run the fix
                        var fixApplied = await context.Database.ExecuteSqlRawAsync(@"
                            SELECT name FROM sqlite_master 
                            WHERE type='table' AND name='_constraint_fix_applied'
                        ");
                        needsFix = false; // Fix already applied
                    }
                    catch
                    {
                        needsFix = true; // Need to apply fix
                    }
                    
                    if (needsFix)
                    {
                        await context.Database.ExecuteSqlRawAsync(@"
                            PRAGMA foreign_keys = OFF;
                            
                            -- Create new table with correct foreign key constraint
                            CREATE TABLE CalendarBookings_fixed (
                                Id TEXT NOT NULL PRIMARY KEY,
                                AreaId TEXT NOT NULL,
                                GroupId TEXT,
                                Date TEXT NOT NULL,
                                StartMin INTEGER NOT NULL,
                                EndMin INTEGER NOT NULL,
                                Notes TEXT,
                                ContactName TEXT,
                                ContactPhone TEXT,
                                ContactEmail TEXT,
                                SourceTemplateId TEXT,
                                CreatedAt TEXT,
                                UpdatedAt TEXT,
                                FOREIGN KEY (AreaId) REFERENCES Areas(Id) ON DELETE CASCADE,
                                FOREIGN KEY (GroupId) REFERENCES Groups(Id) ON DELETE CASCADE,
                                FOREIGN KEY (SourceTemplateId) REFERENCES ScheduleTemplates(Id) ON DELETE SET NULL
                            );
                            
                            -- Copy all data from old table
                            INSERT INTO CalendarBookings_fixed 
                            SELECT Id, AreaId, GroupId, Date, StartMin, EndMin, Notes, 
                                   ContactName, ContactPhone, ContactEmail, SourceTemplateId, 
                                   CreatedAt, UpdatedAt 
                            FROM CalendarBookings;
                            
                            -- Drop old table
                            DROP TABLE CalendarBookings;
                            
                            -- Rename new table to original name
                            ALTER TABLE CalendarBookings_fixed RENAME TO CalendarBookings;
                            
                            -- Recreate indexes
                            CREATE INDEX IF NOT EXISTS IX_CalendarBookings_AreaId ON CalendarBookings(AreaId);
                            CREATE INDEX IF NOT EXISTS IX_CalendarBookings_GroupId ON CalendarBookings(GroupId);
                            CREATE INDEX IF NOT EXISTS IX_CalendarBookings_SourceTemplateId ON CalendarBookings(SourceTemplateId);
                            
                            -- Mark that fix has been applied
                            CREATE TABLE IF NOT EXISTS _constraint_fix_applied (
                                Id INTEGER PRIMARY KEY,
                                AppliedAt TEXT NOT NULL
                            );
                            INSERT INTO _constraint_fix_applied (AppliedAt) VALUES (datetime('now'));
                            
                            PRAGMA foreign_keys = ON;
                        ");
                        
                        Console.WriteLine("[MIGRATION] Fixed SourceTemplateId constraint for SQLite");
                        logger.LogInformation("Fixed SourceTemplateId constraint for SQLite");
                    }
                }
                catch (Exception fixEx)
                {
                    // If the fix fails, it might be because the constraint is already correct
                    // or the table structure is different - log but don't fail
                    Console.WriteLine($"[MIGRATION] Constraint fix (non-critical): {fixEx.Message}");
                    logger.LogInformation("Constraint fix (non-critical): {Message}", fixEx.Message);
                }
                
                // Add Source and DisplayColor columns to Groups table if they don't exist
                // CRITICAL: This must run before any queries that use these columns
                // Works for both SQLite and PostgreSQL
                try
                {
                    var connection = context.Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                        await connection.OpenAsync();
                    
                    bool isPostgres = provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);
                    
                    if (isPostgres)
                    {
                        // PostgreSQL: Use information_schema
                        // Add Source column if it doesn't exist
                        await context.Database.ExecuteSqlRawAsync(@"
                            DO $$ 
                            BEGIN 
                                IF NOT EXISTS (
                                    SELECT 1 FROM information_schema.columns 
                                    WHERE table_name = 'Groups' AND column_name = 'Source'
                                ) THEN
                                    ALTER TABLE ""Groups"" ADD COLUMN ""Source"" VARCHAR(50) NOT NULL DEFAULT 'Egen';
                                END IF;
                            END $$;
                        ");
                        Console.WriteLine("[MIGRATION] ✅ Checked/added Source column to Groups table (PostgreSQL)");
                        logger.LogInformation("✅ Checked/added Source column to Groups table (PostgreSQL)");
                        
                        // Add DisplayColor column if it doesn't exist
                        await context.Database.ExecuteSqlRawAsync(@"
                            DO $$ 
                            BEGIN 
                                IF NOT EXISTS (
                                    SELECT 1 FROM information_schema.columns 
                                    WHERE table_name = 'Groups' AND column_name = 'DisplayColor'
                                ) THEN
                                    ALTER TABLE ""Groups"" ADD COLUMN ""DisplayColor"" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå';
                                END IF;
                            END $$;
                        ");
                        Console.WriteLine("[MIGRATION] ✅ Checked/added DisplayColor column to Groups table (PostgreSQL)");
                        logger.LogInformation("✅ Checked/added DisplayColor column to Groups table (PostgreSQL)");
                        
                        // Update existing groups with default values
                        var updated = await context.Database.ExecuteSqlRawAsync(@"
                            UPDATE ""Groups"" 
                            SET ""Source"" = COALESCE(""Source"", 'Egen'),
                                ""DisplayColor"" = COALESCE(""DisplayColor"", 'Ljusblå')
                            WHERE ""Source"" IS NULL OR ""DisplayColor"" IS NULL
                        ");
                        Console.WriteLine($"[MIGRATION] ✅ Updated {updated} groups with default values (PostgreSQL)");
                        logger.LogInformation("✅ Updated {Count} groups with default values (PostgreSQL)", updated);
                        
                        // Add StandardDisplayColor to GroupTypes if it doesn't exist
                        await context.Database.ExecuteSqlRawAsync(@"
                            DO $$ 
                            BEGIN 
                                IF NOT EXISTS (
                                    SELECT 1 FROM information_schema.columns 
                                    WHERE table_name = 'GroupTypes' AND column_name = 'StandardDisplayColor'
                                ) THEN
                                    ALTER TABLE ""GroupTypes"" ADD COLUMN ""StandardDisplayColor"" VARCHAR(50) NOT NULL DEFAULT 'Ljusblå';
                                END IF;
                            END $$;
                        ");
                        Console.WriteLine("[MIGRATION] ✅ Checked/added StandardDisplayColor column to GroupTypes table (PostgreSQL)");
                        logger.LogInformation("✅ Checked/added StandardDisplayColor column to GroupTypes table (PostgreSQL)");
                        
                        // Create Modals table if it doesn't exist (PostgreSQL)
                        // Use native DATE and TIMESTAMP types for better performance and type safety
                        await context.Database.ExecuteSqlRawAsync(@"
                            DO $$ 
                            BEGIN 
                                IF NOT EXISTS (
                                    SELECT 1 FROM information_schema.tables 
                                    WHERE table_schema = 'public' AND table_name = 'Modals'
                                ) THEN
                                    CREATE TABLE ""Modals"" (
                                        ""Id"" TEXT NOT NULL PRIMARY KEY,
                                        ""Title"" VARCHAR(200) NOT NULL,
                                        ""Content"" TEXT NOT NULL,
                                        ""StartDate"" DATE NOT NULL,
                                        ""EndDate"" DATE NOT NULL,
                                        ""LinkRoute"" VARCHAR(200),
                                        ""ButtonText"" VARCHAR(50),
                                        ""CreatedAt"" TIMESTAMP NOT NULL,
                                        ""UpdatedAt"" TIMESTAMP NOT NULL
                                    );
                                    CREATE INDEX ""IX_Modals_StartDate_EndDate"" ON ""Modals"" (""StartDate"", ""EndDate"");
                                    RAISE NOTICE 'Created Modals table';
                                ELSE
                                    -- Table exists, check if we need to alter columns from TEXT to DATE/TIMESTAMP
                                    IF EXISTS (
                                        SELECT 1 FROM information_schema.columns 
                                        WHERE table_schema = 'public' AND table_name = 'Modals' AND column_name = 'StartDate' AND data_type = 'text'
                                    ) THEN
                                        ALTER TABLE ""Modals"" ALTER COLUMN ""StartDate"" TYPE DATE USING ""StartDate""::DATE;
                                        ALTER TABLE ""Modals"" ALTER COLUMN ""EndDate"" TYPE DATE USING ""EndDate""::DATE;
                                        ALTER TABLE ""Modals"" ALTER COLUMN ""CreatedAt"" TYPE TIMESTAMP USING ""CreatedAt""::TIMESTAMP;
                                        ALTER TABLE ""Modals"" ALTER COLUMN ""UpdatedAt"" TYPE TIMESTAMP USING ""UpdatedAt""::TIMESTAMP;
                                        RAISE NOTICE 'Altered Modals table columns to DATE/TIMESTAMP';
                                    END IF;
                                END IF;
                            END $$;
                        ");
                        
                        // Create ModalReadBy table if it doesn't exist (PostgreSQL)
                        await context.Database.ExecuteSqlRawAsync(@"
                            DO $$ 
                            BEGIN 
                                IF NOT EXISTS (
                                    SELECT 1 FROM information_schema.tables 
                                    WHERE table_schema = 'public' AND table_name = 'ModalReadBy'
                                ) THEN
                                    CREATE TABLE ""ModalReadBy"" (
                                        ""Id"" TEXT NOT NULL PRIMARY KEY,
                                        ""ModalId"" TEXT NOT NULL,
                                        ""UserId"" VARCHAR(450) NOT NULL,
                                        ""ReadAt"" TIMESTAMP NOT NULL,
                                        CONSTRAINT ""FK_ModalReadBy_Modals_ModalId"" FOREIGN KEY (""ModalId"") REFERENCES ""Modals"" (""Id"") ON DELETE CASCADE
                                    );
                                    CREATE INDEX ""IX_ModalReadBy_ModalId_UserId"" ON ""ModalReadBy"" (""ModalId"", ""UserId"");
                                    RAISE NOTICE 'Created ModalReadBy table';
                                ELSE
                                    -- Table exists, check if we need to alter ReadAt column
                                    IF EXISTS (
                                        SELECT 1 FROM information_schema.columns 
                                        WHERE table_schema = 'public' AND table_name = 'ModalReadBy' AND column_name = 'ReadAt' AND data_type = 'text'
                                    ) THEN
                                        ALTER TABLE ""ModalReadBy"" ALTER COLUMN ""ReadAt"" TYPE TIMESTAMP USING ""ReadAt""::TIMESTAMP;
                                        RAISE NOTICE 'Altered ModalReadBy ReadAt column to TIMESTAMP';
                                    END IF;
                                END IF;
                            END $$;
                        ");
                        Console.WriteLine("[MIGRATION] ✅ Checked/created Modals and ModalReadBy tables (PostgreSQL)");
                        logger.LogInformation("✅ Checked/created Modals and ModalReadBy tables (PostgreSQL)");
                    }
                    else
                    {
                        // SQLite: Use PRAGMA
                        var command = connection.CreateCommand();
                        command.CommandText = "PRAGMA table_info(Groups)";
                        var reader = await command.ExecuteReaderAsync();
                        var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        while (await reader.ReadAsync())
                        {
                            var colName = reader.GetString(1); // Column name is at index 1
                            columnNames.Add(colName);
                        }
                        await reader.CloseAsync();
                        
                        var hasSourceColumn = columnNames.Contains("Source");
                        var hasDisplayColorColumn = columnNames.Contains("DisplayColor");
                        
                        if (!hasSourceColumn)
                        {
                            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Groups ADD COLUMN Source TEXT DEFAULT 'Egen'");
                            await context.Database.ExecuteSqlRawAsync("UPDATE Groups SET Source = 'Egen' WHERE Source IS NULL OR Source = ''");
                            Console.WriteLine("[MIGRATION] ✅ Added Source column to Groups table");
                            logger.LogInformation("✅ Added Source column to Groups table");
                        }
                        
                        if (!hasDisplayColorColumn)
                        {
                            await context.Database.ExecuteSqlRawAsync("ALTER TABLE Groups ADD COLUMN DisplayColor TEXT DEFAULT 'Ljusblå'");
                            await context.Database.ExecuteSqlRawAsync("UPDATE Groups SET DisplayColor = 'Ljusblå' WHERE DisplayColor IS NULL OR DisplayColor = ''");
                            Console.WriteLine("[MIGRATION] ✅ Added DisplayColor column to Groups table");
                            logger.LogInformation("✅ Added DisplayColor column to Groups table");
                        }
                        else
                        {
                            Console.WriteLine("[MIGRATION] DisplayColor column already exists");
                        }
                        
                        // Check and add StandardDisplayColor column to GroupTypes table
                        var groupTypesColumnNames = new List<string>();
                        using (var groupTypesCommand = connection.CreateCommand())
                        {
                            groupTypesCommand.CommandText = "SELECT name FROM pragma_table_info('GroupTypes')";
                            using (var groupTypesReader = await groupTypesCommand.ExecuteReaderAsync())
                            {
                                while (await groupTypesReader.ReadAsync())
                                {
                                    groupTypesColumnNames.Add(groupTypesReader.GetString(0));
                                }
                            }
                        }
                        
                        var hasStandardDisplayColorColumn = groupTypesColumnNames.Contains("StandardDisplayColor");
                        if (!hasStandardDisplayColorColumn)
                        {
                            await context.Database.ExecuteSqlRawAsync("ALTER TABLE GroupTypes ADD COLUMN StandardDisplayColor TEXT DEFAULT 'Ljusblå'");
                            await context.Database.ExecuteSqlRawAsync("UPDATE GroupTypes SET StandardDisplayColor = 'Ljusblå' WHERE StandardDisplayColor IS NULL OR StandardDisplayColor = ''");
                            Console.WriteLine("[MIGRATION] ✅ Added StandardDisplayColor column to GroupTypes table");
                            logger.LogInformation("✅ Added StandardDisplayColor column to GroupTypes table");
                        }
                        else
                        {
                            Console.WriteLine("[MIGRATION] StandardDisplayColor column already exists");
                        }
                        
                        // Check and create Modals table if it doesn't exist (SQLite)
                        var modalsTableExists = false;
                        using (var modalsCheckCommand = connection.CreateCommand())
                        {
                            modalsCheckCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Modals'";
                            using (var modalsCheckReader = await modalsCheckCommand.ExecuteReaderAsync())
                            {
                                modalsTableExists = await modalsCheckReader.ReadAsync();
                            }
                        }
                        
                        if (!modalsTableExists)
                        {
                            await context.Database.ExecuteSqlRawAsync(@"
                                CREATE TABLE IF NOT EXISTS Modals (
                                    Id TEXT NOT NULL PRIMARY KEY,
                                    Title TEXT NOT NULL,
                                    Content TEXT NOT NULL,
                                    StartDate TEXT NOT NULL,
                                    EndDate TEXT NOT NULL,
                                    LinkRoute TEXT,
                                    ButtonText TEXT,
                                    CreatedAt TEXT NOT NULL,
                                    UpdatedAt TEXT NOT NULL
                                );
                                CREATE INDEX IF NOT EXISTS IX_Modals_StartDate_EndDate ON Modals(StartDate, EndDate);
                            ");
                            Console.WriteLine("[MIGRATION] ✅ Created Modals table (SQLite)");
                            logger.LogInformation("✅ Created Modals table (SQLite)");
                        }
                        
                        // Check and create ModalReadBy table if it doesn't exist (SQLite)
                        var modalReadByTableExists = false;
                        using (var modalReadByCheckCommand = connection.CreateCommand())
                        {
                            modalReadByCheckCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='ModalReadBy'";
                            using (var modalReadByCheckReader = await modalReadByCheckCommand.ExecuteReaderAsync())
                            {
                                modalReadByTableExists = await modalReadByCheckReader.ReadAsync();
                            }
                        }
                        
                        if (!modalReadByTableExists)
                        {
                            await context.Database.ExecuteSqlRawAsync(@"
                                CREATE TABLE IF NOT EXISTS ModalReadBy (
                                    Id TEXT NOT NULL PRIMARY KEY,
                                    ModalId TEXT NOT NULL,
                                    UserId TEXT NOT NULL,
                                    ReadAt TEXT NOT NULL,
                                    FOREIGN KEY (ModalId) REFERENCES Modals(Id) ON DELETE CASCADE
                                );
                                CREATE INDEX IF NOT EXISTS IX_ModalReadBy_ModalId_UserId ON ModalReadBy(ModalId, UserId);
                            ");
                            Console.WriteLine("[MIGRATION] ✅ Created ModalReadBy table (SQLite)");
                            logger.LogInformation("✅ Created ModalReadBy table (SQLite)");
                        }
                    }
                    
                    if (connection.State == System.Data.ConnectionState.Open)
                        await connection.CloseAsync();
                }
                catch (Exception colEx)
                {
                    Console.WriteLine($"[MIGRATION] ❌ Column addition failed: {colEx.Message}");
                    Console.WriteLine($"[MIGRATION] Stack trace: {colEx.StackTrace}");
                    logger.LogError(colEx, "❌ Column addition failed: {Message}", colEx.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MIGRATION] ERROR: Failed to migrate AppDbContext: {ex.Message}");
            logger.LogError(ex, "=== FAILED to migrate AppDbContext: {Message} ===", ex.Message);
        }
        
        try
        {
            Console.WriteLine("[SEED] Starting database seed...");
            logger.LogInformation("=== Starting database seed ===");
            var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
            await seeder.SeedAsync();
            Console.WriteLine("[SEED] Database seed completed");
            logger.LogInformation("=== Database seed completed ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SEED] ERROR: Failed to seed database: {ex.Message}");
            logger.LogError(ex, "=== FAILED to seed database: {Message} ===", ex.Message);
        }

        // One-time data ownership fix: attach legacy data without UserId to admin account
        // CRITICAL: This ensures groups are visible to users
        try
        {
            const string adminEmail = "admin@sportadmin.se";
            var adminUser = await identityContext.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (adminUser != null)
            {
                var adminId = adminUser.Id;
                var fixedGroups = await context.Database.ExecuteSqlRawAsync("UPDATE Groups SET UserId = {0} WHERE UserId IS NULL OR TRIM(UserId) = ''", adminId);
                var fixedTemplates = await context.Database.ExecuteSqlRawAsync("UPDATE ScheduleTemplates SET UserId = {0} WHERE UserId IS NULL OR TRIM(UserId) = ''", adminId);
                var fixedPlaces = await context.Database.ExecuteSqlRawAsync("UPDATE Places SET UserId = {0} WHERE UserId IS NULL OR TRIM(UserId) = ''", adminId);
                Console.WriteLine($"[SEED] Fixed UserId: {fixedGroups} groups, {fixedTemplates} templates, {fixedPlaces} places");
                logger.LogInformation("Fixed UserId: {Groups} groups, {Templates} templates, {Places} places", fixedGroups, fixedTemplates, fixedPlaces);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SEED] Error fixing UserId: {ex.Message}");
            logger.LogError(ex, "Error fixing UserId");
        }

        // One-time data fix: rename legacy group "Herr U" -> "P19" and set type to "Akademi"
        try
        {
            await context.Database.ExecuteSqlRawAsync("UPDATE Groups SET Name = 'P19' WHERE LOWER(Name) = 'herr u';");
            await context.Database.ExecuteSqlRawAsync("UPDATE Groups SET GroupType = 'Akademi' WHERE LOWER(Name) = 'p19';");
        }
        catch { }

        // One-time data fix: remove invalid DayOfWeek entries (e.g., 8) in template "Veckoschema HT2025"
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"DELETE FROM BookingTemplates 
                WHERE ScheduleTemplateId = '25EB47F8-AC32-4656-B253-355F6806B4EB' 
                  AND (DayOfWeek < 1 OR DayOfWeek > 7);");
        }
        catch { }

        // Ensure GroupTypes table exists (lightweight bootstrap without full migration)
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"CREATE TABLE IF NOT EXISTS GroupTypes (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                UserId TEXT NULL
            );");
        }
        catch { }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        Console.WriteLine($"[BACKGROUND] ERROR: {ex.Message}");
        logger.LogError(ex, "Background migration/seed error: {Message}", ex.Message);
    }
    finally
    {
        scope?.Dispose();
    }
});

// --- Debug endpoint for modals ---
app.MapGet("/debug/modals", async (HttpContext context) =>
{
    try
    {
        var scope = context.RequestServices.CreateScope();
        var modalService = scope.ServiceProvider.GetRequiredService<IModalService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var provider = db.Database.ProviderName ?? "unknown";
        var canConnect = await db.Database.CanConnectAsync();
        
        // Check what tables exist
        List<string> existingTables = new();
        string? tableCheckError = null;
        try
        {
            if (provider.Contains("Npgsql"))
            {
                var tables = await db.Database.SqlQueryRaw<string>(
                    "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND (table_name = 'Modals' OR table_name = 'modals' OR table_name = 'ModalReadBy' OR table_name = 'modalreadby')"
                ).ToListAsync();
                existingTables = tables;
            }
        }
        catch (Exception ex)
        {
            tableCheckError = $"{ex.GetType().Name}: {ex.Message}";
        }
        
        // Try to query directly via EF
        int directCount = 0;
        string? directError = null;
        try
        {
            directCount = await db.Modals.CountAsync();
        }
        catch (Exception ex)
        {
            directError = $"{ex.GetType().Name}: {ex.Message}";
        }
        
        // Try raw SQL query
        int rawSqlCount = 0;
        string? rawSqlError = null;
        try
        {
            if (provider.Contains("Npgsql"))
            {
                rawSqlCount = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) FROM \"Modals\"").FirstOrDefaultAsync();
            }
        }
        catch (Exception ex)
        {
            rawSqlError = $"{ex.GetType().Name}: {ex.Message}";
        }
        
        // Try service
        List<Modal> allModals = new();
        string? serviceError = null;
        try
        {
            allModals = await modalService.GetAllModalsAsync();
        }
        catch (Exception ex)
        {
            serviceError = $"{ex.GetType().Name}: {ex.Message}";
        }
        
        scope.Dispose();
        
        return Results.Ok(new 
        { 
            databaseProvider = provider,
            canConnect = canConnect,
            existingTables = existingTables,
            tableCheckError = tableCheckError,
            modalsFromService = allModals.Count,
            serviceError = serviceError,
            modalsFromDirectQuery = directCount,
            directQueryError = directError,
            modalsFromRawSql = rawSqlCount,
            rawSqlError = rawSqlError,
            modals = allModals.Select(m => new { m.Id, m.Title, m.StartDate, m.EndDate }).ToList(),
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Debug endpoint failed: {ex.Message}\n{ex.StackTrace}");
    }
});

// --- Health check endpoint ---
app.MapGet("/health", async (HttpContext context) =>
{
    try
    {
        var scope = context.RequestServices.CreateScope();
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        
        // Check connection string
        var connString1 = configuration.GetConnectionString("DefaultConnection");
        var connString2 = configuration["ConnectionStrings:DefaultConnection"];
        var connString3 = configuration["ConnectionStrings__DefaultConnection"];
        
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        string dbError = null;
        string dbProvider = null;
        int modalCount = 0;
        try
        {
            var canConnect = await db.Database.CanConnectAsync();
            dbProvider = db.Database.ProviderName ?? "unknown";
            
            // Try to count modals
            try
            {
                modalCount = await db.Modals.CountAsync();
            }
            catch (Exception ex)
            {
                dbError = $"Can connect but error reading Modals: {ex.Message}";
            }
        }
        catch (Exception ex)
        {
            dbError = ex.Message;
        }
        
        var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        string identityError = null;
        try
        {
            var canConnectIdentity = await identityDb.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            identityError = ex.Message;
        }
        
        scope.Dispose();
        
        return Results.Ok(new 
        { 
            status = "healthy",
            database = dbError == null ? "connected" : $"disconnected: {dbError}",
            databaseProvider = dbProvider,
            modalCount = modalCount,
            identityDb = identityError == null ? "connected" : $"disconnected: {identityError}",
            connStringFound = !string.IsNullOrEmpty(connString1) || !string.IsNullOrEmpty(connString2) || !string.IsNullOrEmpty(connString3),
            connString1 = !string.IsNullOrEmpty(connString1),
            connString2 = !string.IsNullOrEmpty(connString2),
            connString3 = !string.IsNullOrEmpty(connString3),
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Health check failed: {ex.Message}\n{ex.StackTrace}");
    }
});

// --- Auth endpoints ---
app.MapPost("/auth/login", async (HttpContext httpContext, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) =>
{
    var logger = httpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    try
    {
        // Read form data - Azure App Service may have different content type handling
        if (!httpContext.Request.HasFormContentType)
        {
            // Try to read anyway - sometimes Azure proxy modifies headers
            logger.LogWarning("Login request without form content type: {ContentType}", httpContext.Request.ContentType);
        }

        var form = await httpContext.Request.ReadFormAsync();
        var email = form["email"].ToString();
        var password = form["password"].ToString();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Results.Redirect("/login?error=missing");
        logger.LogInformation("Login attempt for email: {Email}", email);
        
        // Check if user exists and has password set
        var user = await userManager.FindByEmailAsync(email);
        if (user != null && string.IsNullOrEmpty(user.PasswordHash))
        {
            logger.LogWarning("Login attempt for unactivated user: {Email}", email);
            return Results.Redirect("/login?error=notactivated");
        }
        
        if (user != null && !user.EmailConfirmed)
        {
            logger.LogWarning("Login attempt for unconfirmed email: {Email}", email);
            return Results.Redirect("/login?error=emailnotconfirmed");
        }
        
        // PasswordSignInAsync can use email directly (Identity supports this)
        // But we also need to find the user to update LastLoginAt
        var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);
        
        if (result.Succeeded)
        {
            logger.LogInformation("Login successful for email: {Email}", email);
            // User was already found above, reuse it
            if (user != null)
            {
                user.LastLoginAt = DateTimeOffset.UtcNow;
                await userManager.UpdateAsync(user);
            }
            return Results.Redirect("/");
        }
        
        logger.LogWarning("Login failed for email: {Email}, Result: {Result}", email, result);
        return Results.Redirect("/login?error=invalid");
    }
    catch (Exception ex)
    {
        // Log error with details for debugging
        logger.LogError(ex, "Error in /auth/login endpoint: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
        return Results.Redirect("/login?error=invalid");
    }
});

app.MapPost("/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});
app.MapGet("/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
});

app.MapPost("/auth/forgot-password", async (HttpContext httpContext, UserManager<ApplicationUser> userManager, IEmailService emailService) =>
{
    var logger = httpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    
    try
    {
        if (!httpContext.Request.HasFormContentType)
        {
            logger.LogWarning("Forgot password request without form content type: {ContentType}", httpContext.Request.ContentType);
        }

        var form = await httpContext.Request.ReadFormAsync();
        var email = form["email"].ToString();

        if (string.IsNullOrWhiteSpace(email))
        {
            // Always return success for security (don't reveal if user exists)
            return Results.Ok(new { success = true, message = "If a user with that email exists, a password reset link has been sent." });
        }

        logger.LogInformation("Password reset requested for email: {Email}", email);

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Always return success for security (don't reveal if user exists)
            logger.LogInformation("Password reset requested for non-existent email: {Email}", email);
            return Results.Ok(new { success = true, message = "If a user with that email exists, a password reset link has been sent." });
        }

        // Generate password reset token (valid for 1 hour)
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        
        // Get base URL for reset link
        var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
        
        // Send password reset email
        try
        {
            await emailService.SendPasswordResetEmailAsync(email, token, baseUrl);
            // Note: EmailService logs success/failure internally
        }
        catch (Exception emailEx)
        {
            logger.LogError(emailEx, "Error sending password reset email to {Email}", email);
            // Still return success for security
        }

        // Always return success for security (don't reveal if user exists)
        return Results.Ok(new { success = true, message = "If a user with that email exists, a password reset link has been sent." });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in /auth/forgot-password endpoint: {Message}", ex.Message);
        // Return success even on error for security
        return Results.Ok(new { success = true, message = "If a user with that email exists, a password reset link has been sent." });
    }
});

app.MapPost("/auth/reset-password", async (HttpContext httpContext, UserManager<ApplicationUser> userManager) =>
{
    var logger = httpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    
    try
    {
        if (!httpContext.Request.HasFormContentType)
        {
            logger.LogWarning("Reset password request without form content type: {ContentType}", httpContext.Request.ContentType);
        }

        var form = await httpContext.Request.ReadFormAsync();
        var email = form["email"].ToString();
        var token = form["token"].ToString();
        var password = form["password"].ToString();
        var confirmPassword = form["confirmPassword"].ToString();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(password))
        {
            return Results.Redirect("/reset-password?error=missing&email=" + Uri.EscapeDataString(email ?? "") + "&token=" + Uri.EscapeDataString(token ?? ""));
        }

        if (password != confirmPassword)
        {
            return Results.Redirect("/reset-password?error=passwordmismatch&email=" + Uri.EscapeDataString(email) + "&token=" + Uri.EscapeDataString(token));
        }

        if (password.Length < 8)
        {
            return Results.Redirect("/reset-password?error=passwordtooshort&email=" + Uri.EscapeDataString(email) + "&token=" + Uri.EscapeDataString(token));
        }

        logger.LogInformation("Password reset attempt for email: {Email}", email);

        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            logger.LogWarning("Password reset attempted for non-existent user: {Email}", email);
            return Results.Redirect("/reset-password?error=invalid&email=" + Uri.EscapeDataString(email) + "&token=" + Uri.EscapeDataString(token));
        }

        // Reset password using token
        var result = await userManager.ResetPasswordAsync(user, token, password);
        
        if (result.Succeeded)
        {
            logger.LogInformation("Password reset successful for email: {Email}", email);
            return Results.Redirect("/login?reset=success");
        }
        else
        {
            logger.LogWarning("Password reset failed for email: {Email}, Errors: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Results.Redirect("/reset-password?error=invalidtoken&email=" + Uri.EscapeDataString(email) + "&token=" + Uri.EscapeDataString(token));
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error in /auth/reset-password endpoint: {Message}", ex.Message);
        return Results.Redirect("/reset-password?error=error");
    }
});

app.Run();
