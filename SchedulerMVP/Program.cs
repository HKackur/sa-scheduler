using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using SchedulerMVP.Data;
using SchedulerMVP.Data.Entities;
using SchedulerMVP.Data.Seed;
using SchedulerMVP.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Get connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Add ApplicationDbContext for Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite("Data Source=app.db");
    }
}, ServiceLifetime.Transient);

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

// Persist login for 7 days by default
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
});

// Add AppDbContext for application data
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite("Data Source=app.db");
    }
}, ServiceLifetime.Transient);

// Add HttpContextAccessor for user context in services
builder.Services.AddHttpContextAccessor();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// 🧩 FIX: remove UseHttpsRedirection() to prevent crash on Fly.io
if (!app.Environment.IsDevelopment())
{
    // Fly.io already terminates HTTPS; avoid redirect-loop crash
    app.Use((ctx, next) =>
    {
        if (ctx.Request.Headers.TryGetValue("Fly-Forwarded-Proto", out var proto) && proto == "https")
            ctx.Request.Scheme = "https";
        return next();
    });
}

app.UseStaticFiles();
app.UseRouting();

// Proxy headers (required for Fly.io / reverse proxy)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// --- Authentication endpoints ---
app.MapPost("/auth/login", async (HttpContext httpContext, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager) =>
{
    var form = await httpContext.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/login?error=missing");
    }

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

// Impersonation (Admin only)
app.MapPost("/auth/impersonate/{userId}", async (string userId, HttpContext http, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) =>
{
    if (!http.User.IsInRole("Admin")) return Results.Forbid();
    var target = await userManager.FindByIdAsync(userId);
    if (target == null) return Results.NotFound();

    var impersonatorId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
    var claims = new List<Claim> { new Claim("ImpersonatorId", impersonatorId ?? string.Empty) };

    await signInManager.SignInWithClaimsAsync(target, isPersistent: false, claims);
    return Results.Redirect("/");
});

app.MapPost("/auth/revert", async (HttpContext http, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) =>
{
    var originalId = http.User.FindFirst("ImpersonatorId")?.Value;
    if (string.IsNullOrEmpty(originalId)) return Results.Redirect("/");

    var admin = await userManager.FindByIdAsync(originalId);
    if (admin == null) return Results.Redirect("/");

    await signInManager.SignInAsync(admin, isPersistent: false);
    return Results.Redirect("/");
});

// Admin grant/revoke
app.MapPost("/auth/grant-admin", async (HttpContext http, string email, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) =>
{
    if (!http.User.IsInRole("Admin") && !http.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        return Results.Forbid();

    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));

    var user = await userManager.FindByEmailAsync(email);
    if (user == null) return Results.NotFound();
    await userManager.AddToRoleAsync(user, "Admin");
    return Results.Ok();
});

app.MapGet("/auth/grant-admin/{email}", async (HttpContext http, string email, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager) =>
{
    if (!http.User.IsInRole("Admin") && !http.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        return Results.Forbid();
    if (!await roleManager.RoleExistsAsync("Admin"))
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    var user = await userManager.FindByEmailAsync(email);
    if (user == null) return Results.NotFound();
    await userManager.AddToRoleAsync(user, "Admin");
    return Results.Ok();
});

app.MapPost("/auth/revoke-admin", async (HttpContext http, string email, UserManager<ApplicationUser> userManager) =>
{
    if (!http.User.IsInRole("Admin") && !http.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        return Results.Forbid();
    var user = await userManager.FindByEmailAsync(email);
    if (user == null) return Results.NotFound();
    await userManager.RemoveFromRoleAsync(user, "Admin");
    return Results.Ok();
});

// Dev-only: reset admin password
if (app.Environment.IsDevelopment())
{
    app.MapPost("/auth/dev-reset-admin", async (UserManager<ApplicationUser> userManager) =>
    {
        const string adminEmail = "admin@sportadmin.se";
        const string newPassword = "Test1234!";
        var user = await userManager.FindByEmailAsync(adminEmail);
        if (user is null) return Results.NotFound("Admin user not found");

        var hasPassword = await userManager.HasPasswordAsync(user);
        if (hasPassword)
        {
            var remove = await userManager.RemovePasswordAsync(user);
            if (!remove.Succeeded)
                return Results.BadRequest(string.Join(", ", remove.Errors.Select(e => e.Description)));
        }

        var add = await userManager.AddPasswordAsync(user, newPassword);
        if (!add.Succeeded)
            return Results.BadRequest(string.Join(", ", add.Errors.Select(e => e.Description)));

        return Results.Ok("Admin password reset");
    });
}

// --- Migrate and seed databases ---
using (var scope = app.Services.CreateScope())
{
    var identityContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await identityContext.Database.MigrateAsync();

    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        var provider = context.Database.ProviderName ?? string.Empty;
        if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            await context.Database.MigrateAsync();
        else
            await context.Database.EnsureCreatedAsync();
    }
    catch { }

    var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
    await seeder.SeedAsync();

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

    try
    {
        await context.Database.ExecuteSqlRawAsync("UPDATE Groups SET Name = 'P19' WHERE LOWER(Name) = 'herr u';");
        await context.Database.ExecuteSqlRawAsync("UPDATE Groups SET GroupType = 'Akademi' WHERE LOWER(Name) = 'p19';");
    }
    catch { }

    try
    {
        await context.Database.ExecuteSqlRawAsync(@"DELETE FROM BookingTemplates 
            WHERE ScheduleTemplateId = '25EB47F8-AC32-4656-B253-355F6806B4EB' 
              AND (DayOfWeek < 1 OR DayOfWeek > 7);");
    }
    catch { }

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

// --- FIX: Fly.io requires explicit port binding ---
app.Urls.Add("http://0.0.0.0:8080");

app.Run();
