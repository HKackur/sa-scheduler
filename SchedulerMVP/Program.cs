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

// Fix Azure culture issue - set default culture to avoid "__10" invalid culture error
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var builder = WebApplication.CreateBuilder(args);

// Force IPv4 for Supabase connections (Azure Basic B1 doesn't support IPv6)
// Configure Npgsql to prefer IPv4
var supabaseHost = "db.anebyqfrzsuqwrbncwxt.supabase.co";

// DataProtection: Persist keys across app restarts for authentication cookies
// Supports both Fly.io (/app/keys) and Azure App Service (local temp path)
if (!builder.Environment.IsDevelopment())
{
    // Try Fly.io path first (if mounted volume exists)
    var flyKeysPath = "/app/keys";
    // Azure App Service: Use local directory (persists across restarts within same instance)
    var azureKeysPath = Path.Combine(Path.GetTempPath(), "SchedulerMVP-Keys");
    
    if (Directory.Exists(flyKeysPath))
    {
        // Fly.io: Use mounted volume
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(flyKeysPath))
            .SetApplicationName("SchedulerMVP");
    }
    else
    {
        // Azure App Service or other platforms: Use local directory
        // Ensure directory exists
        if (!Directory.Exists(azureKeysPath))
        {
            Directory.CreateDirectory(azureKeysPath);
        }
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(azureKeysPath))
            .SetApplicationName("SchedulerMVP");
    }
    // If both fail, DataProtection uses in-memory keys (cookies invalid on restart)
}

// Port configuration: Supports both Fly.io (8080) and Azure App Service
// Azure App Service sets PORT environment variable - app MUST listen on that port
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port) && int.TryParse(port, out var portNumber))
{
    // Azure App Service (or any platform with PORT env var): Use the provided port
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(portNumber);
        // Support both HTTP/1.1 and HTTP/2 for compatibility
        options.ConfigureEndpointDefaults(lo => lo.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
    });
}
else
{
    // Fly.io or local dev: Use default 8080
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(8080);
        // Support both HTTP/1.1 and HTTP/2 for compatibility
        options.ConfigureEndpointDefaults(lo => lo.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
    });
}

// Add services to the container.
builder.Services.AddRazorPages();
// CRITICAL: Configure SignalR BEFORE AddServerSideBlazor for Blazor Server on Fly.io
// This ensures proper connection handling behind proxy
builder.Services.AddSignalR(options =>
{
    // Increased timeouts for Fly.io proxy/network latency
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.EnableDetailedErrors = builder.Environment.IsDevelopment(); // Enable in development for debugging
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB max message size
});

builder.Services.AddServerSideBlazor(options =>
{
    // Enable detailed errors to debug Azure issues
    options.DetailedErrors = true; // Enable in production to see what's causing crashes
    options.DisconnectedCircuitMaxRetained = 100;
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    options.MaxBufferedUnacknowledgedRenderBatches = 20;
});

// Get connection string - try both Azure Connection String format and App Settings format
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? builder.Configuration["ConnectionStrings__DefaultConnection"];

// Connection string should be set correctly in Azure App Service
// For Session pooler: use aws-1-eu-west-1.pooler.supabase.com:5432
// For Transaction pooler: use aws-1-eu-west-1.pooler.supabase.com:6543
// Don't modify connection string here - use exact value from Azure Portal

// Log connection string status (without exposing password)
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("[ERROR] Connection string is null or empty!");
}
else
{
    var host = connectionString.Split(';').FirstOrDefault(s => s.StartsWith("Host="))?.Replace("Host=", "") ?? "unknown";
    var connectionPort = connectionString.Split(';').FirstOrDefault(s => s.StartsWith("Port="))?.Replace("Port=", "") ?? "unknown";
    Console.WriteLine($"[INFO] Connection string found, Host: {host}, Port: {connectionPort}");
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

// Persist login for 30 days
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
    // Required for HTTPS/proxy environments (Fly.io)
    options.Cookie.SameSite = SameSiteMode.Lax; // Use Lax for better compatibility
    // CRITICAL: Use Always in production (HTTPS) - Fly.io uses HTTPS proxy
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    // CRITICAL: MaxAge ensures cookie persists across browser restarts
    options.Cookie.MaxAge = TimeSpan.FromDays(30);
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
builder.Services.AddScoped<ICalendarBookingService, CalendarBookingService>();
builder.Services.AddScoped<BookingDialogService>();
builder.Services.AddScoped<UIState>();
builder.Services.AddScoped<DbSeeder>();
builder.Services.AddScoped<UserContextService>();
builder.Services.AddScoped<IGroupTypeService, GroupTypeService>();
builder.Services.AddScoped<RoleManager<IdentityRole>>();

var app = builder.Build();

// Respect proxy headers: Supports both Fly.io and Azure App Service
// CRITICAL: Include XForwardedHost for SignalR base path detection
// Azure App Service uses X-Forwarded-* headers, Fly.io uses Fly-Forwarded-* headers
// MUST be called before UseStaticFiles and other middleware
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    // Clear known networks/proxies to allow both Fly.io and Azure App Service proxies
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

// Additional HTTPS scheme detection for Fly.io (UseForwardedHeaders handles Azure)
// Fly.io uses Fly-Forwarded-Proto instead of X-Forwarded-Proto
app.Use((ctx, next) =>
{
    // Fly.io specific header
    if (ctx.Request.Headers.TryGetValue("Fly-Forwarded-Proto", out var flyProto) && flyProto == "https")
    {
        ctx.Request.Scheme = "https";
    }
    return next();
});

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

// Configure Blazor Server SignalR hub
app.MapBlazorHub(options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets | 
                         Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(90);
});

app.MapFallbackToPage("/_Host");

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
        try
        {
            var canConnect = await db.Database.CanConnectAsync();
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
            identityDb = identityError == null ? "connected" : $"disconnected: {identityError}",
            connStringFound = !string.IsNullOrEmpty(connString1) || !string.IsNullOrEmpty(connString2) || !string.IsNullOrEmpty(connString3),
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
        
        logger.LogInformation("Login attempt for email: {Email}, Password length: {Length}", email, password?.Length ?? 0);
        
        // Find user first to verify they exist
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            logger.LogWarning("Login failed - user not found for email: {Email}", email);
            return Results.Redirect("/login?error=invalid");
        }
        
        logger.LogInformation("User found: {Email}, UserName: {UserName}, HasPassword: {HasPassword}", 
            user.Email, user.UserName, !string.IsNullOrEmpty(user.PasswordHash));
        
        // Test password directly
        var passwordValid = await userManager.CheckPasswordAsync(user, password);
        logger.LogInformation("Password check result: {Valid}", passwordValid);
        
        // Try sign in with username (Identity needs username, not email)
        var result = await signInManager.PasswordSignInAsync(user.UserName!, password, isPersistent: true, lockoutOnFailure: false);
        
        if (result.Succeeded)
        {
            logger.LogInformation("Login successful for email: {Email}", email);
            user.LastLoginAt = DateTimeOffset.UtcNow;
            await userManager.UpdateAsync(user);
            return Results.Redirect("/");
        }
        
        logger.LogWarning("Login failed for email: {Email}, Result: {Result}, IsLockedOut: {Locked}, RequiresVerification: {Verify}", 
            email, result, result.IsLockedOut, result.RequiresTwoFactor);
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

// --- DB migration & seed ---
var scope = app.Services.CreateScope();
try
{
    // Get contexts outside try-catch so they're available for later use
    var identityContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    Console.WriteLine("[MIGRATION] Starting Identity database migration...");
    logger.LogInformation("=== Starting Identity database migration ===");
    
    try
    {
        // Migrate Identity database
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
    }
    catch (Exception ex)
    {
        // Log but don't crash - app should start even if migrations fail
        Console.WriteLine($"[MIGRATION] ERROR: Failed to migrate Identity database: {ex.Message}");
        Console.WriteLine($"[MIGRATION] Stack: {ex.StackTrace}");
        logger.LogError(ex, "=== FAILED to migrate Identity database: {Message} ===", ex.Message);
        logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
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
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[MIGRATION] ERROR: Failed to migrate AppDbContext: {ex.Message}");
        Console.WriteLine($"[MIGRATION] Stack: {ex.StackTrace}");
        logger.LogError(ex, "=== FAILED to migrate AppDbContext: {Message} ===", ex.Message);
        logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
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
        // Log but don't crash - app should start even if seeding fails
        Console.WriteLine($"[SEED] ERROR: Failed to seed database: {ex.Message}");
        Console.WriteLine($"[SEED] Stack: {ex.StackTrace}");
        logger.LogError(ex, "=== FAILED to seed database: {Message} ===", ex.Message);
        logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
        // Don't rethrow - let app start anyway
    }

    // One-time data ownership fix: attach legacy data without UserId to admin account
    try
    {
        const string adminEmail = "admin@sportadmin.se";
        var adminUser = await identityContext.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (adminUser != null)
        {
            var adminId = adminUser.Id;
            await context.Database.ExecuteSqlRawAsync("UPDATE Groups SET UserId = {0} WHERE UserId IS NULL OR TRIM(UserId) = ''", adminId);
            await context.Database.ExecuteSqlRawAsync("UPDATE ScheduleTemplates SET UserId = {0} WHERE UserId IS NULL OR TRIM(UserId) = ''", adminId);
            await context.Database.ExecuteSqlRawAsync("UPDATE Places SET UserId = {0} WHERE UserId IS NULL OR TRIM(UserId) = ''", adminId);
        }
    }
    catch { }

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
finally
{
    scope?.Dispose();
}

app.Run();
