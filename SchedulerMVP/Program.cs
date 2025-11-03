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

var builder = WebApplication.CreateBuilder(args);

// CRITICAL: Configure DataProtection to persist keys across app restarts on Fly.io
// This ensures authentication cookies can be decrypted even after deployment/restart
// Keys are stored in /app/keys (mounted volume on Fly.io)
if (!builder.Environment.IsDevelopment())
{
    var keysPath = "/app/keys";
    if (Directory.Exists(keysPath))
    {
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
            .SetApplicationName("SchedulerMVP");
    }
    // If directory doesn't exist, DataProtection will use in-memory keys (works but cookies invalid on restart)
}

// Explicitly tell Kestrel to listen on Fly.io port
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

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
    options.EnableDetailedErrors = false; // Disable in production for security
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB max message size
});

builder.Services.AddServerSideBlazor(options =>
{
    // Disable circuit disconnect timeout for better reliability
    options.DetailedErrors = false;
    options.DisconnectedCircuitMaxRetained = 100;
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(1);
    options.MaxBufferedUnacknowledgedRenderBatches = 20;
});

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add ApplicationDbContext for Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly("SchedulerMVP");
            // Configure for better reliability in production
            npgsql.CommandTimeout(30); // 30 second timeout
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
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
            // Configure for better reliability in production
            npgsql.CommandTimeout(30); // 30 second timeout
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
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
            npgsql.CommandTimeout(30);
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
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

// Respect proxy headers (Fly.io / reverse proxies) - MUST be before UseHttpsRedirection
// CRITICAL: Include XForwardedHost for SignalR base path detection
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    // Clear known networks/proxies to allow Fly.io's proxy
    KnownNetworks = { },
    KnownProxies = { }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    
    // Fly.io hanterar redan HTTPS – undvik redirect-loop
    app.Use((ctx, next) =>
    {
        if (ctx.Request.Headers.TryGetValue("Fly-Forwarded-Proto", out var proto) && proto == "https")
            ctx.Request.Scheme = "https";
        return next();
    });
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Configure Blazor Server SignalR hub with proper transport options for Fly.io
// CRITICAL: Long Polling is more reliable than WebSockets behind Fly.io proxy
app.MapBlazorHub(options =>
{
    // Prioritize Long Polling over WebSockets for better reliability behind proxy
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling | 
                         Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
    // Increased poll timeout for better stability on slower connections
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(90);
});

app.MapFallbackToPage("/_Host");

// --- Auth endpoints ---
app.MapPost("/auth/login", async (HttpContext httpContext, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        return Results.Redirect("/login?error=missing");

    var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);
    if (result.Succeeded)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            user.LastLoginAt = DateTimeOffset.UtcNow;
            await userManager.UpdateAsync(user);
        }
        return Results.Redirect("/");
    }

    return Results.Redirect("/login?error=invalid");
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
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to seed database");
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
