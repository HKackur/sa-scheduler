using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;
using SchedulerMVP.Data.Seed;
using SchedulerMVP.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Explicitly tell Kestrel to listen on Fly.io port
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options =>
{
    // Enable detailed errors temporarily to diagnose production circuit crash
    options.DetailedErrors = true;
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

// Persist login for 30 days
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
    // Required for HTTPS/proxy environments (Fly.io)
    options.Cookie.SameSite = SameSiteMode.Lax; // Use Lax for better compatibility
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Works with both HTTP and HTTPS
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".AspNetCore.Identity.Application";
    options.Cookie.IsEssential = true; // Required for authentication
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

// Add SignalR options for better reliability (MUST be before AddServerSideBlazor)
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.StreamBufferCapacity = 10;
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});

// Add DbContextFactory for thread-safe DbContext access in Blazor Server
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
});

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
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Configure Blazor Server SignalR hub with proper transport options for Fly.io
// CRITICAL: Map Blazor hub BEFORE fallback route
var blazorHub = app.MapBlazorHub(options =>
{
    // Enable WebSockets and Long Polling for better reliability behind proxies
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets | 
                         Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
    
    // Increase timeout for slow connections
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(30);
});

app.MapFallbackToPage("/_Host");

// Log when Blazor hub is accessed (must be after MapFallbackToPage)
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/_blazor"))
    {
        Console.WriteLine($"[SignalR] Blazor hub request: {context.Request.Path} - Method: {context.Request.Method}");
    }
    await next();
});

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
